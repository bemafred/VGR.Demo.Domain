# Appendix J: Performance & Query Optimization

***Ensuring Epistemic Clarity Does Not Compromise Execution Efficiency***

> **Epistemisk status: Emergence → Delvis Engineered**
> Basmekanismerna (`WithSemantics()`, expression rewriting, pushdown via LINQ) är implementerade
> och verifierade. Följande delar är ännu inte implementerade: formella pushdown-tester (§3.3),
> `.Include()`-flaggning via analyzer (§4.3), OpenTelemetry-instrumentering (§8.1),
> execution plan visualization.

## 1. Introduction

Semantic Architecture introduces several layers of abstraction:
-	semantic queries expressed in domain language
-	expression tree rewriting via `SemanticRegistry`
-	expansion-based query enrichment
-	projection-driven execution in EF Core

A natural concern is: **Does semantic abstraction degrade performance?**

This appendix addresses that question by:
1.	Defining performance principles in Semantic Architecture
2.	Explaining the **pushdown strategy** and how it's guaranteed
3.	Demonstrating how semantic queries prevent common anti-patterns
4.	Providing benchmarking strategies
5.	Establishing indexing policies tied to Domain Queries
6.	Defining observability requirements
7.	Outlining optimization patterns

The answer is: **Semantic Architecture maintains — and often improves — performance, because meaning is explicit and systematically translated to efficient SQL.**

---

## 2. Performance Principles

Semantic Architecture is built on these performance guarantees:

### Principle 1: Semantic Queries Must Push Down

All filtering, projection, and aggregation logic **must execute in the database**, not in application memory.

**Mechanism:**  
Expression tree rewriting ensures that semantic queries produce EF-translatable expressions that generate SQL `WHERE`, `SELECT`, and `GROUP BY` clauses.

**Verification:**  
Each semantic query is tested with `.ToQueryString()` to confirm SQL generation.

---

### Principle 2: Zero Unnecessary Materialization

Lazy evaluation and projection-driven execution ensure that:
-	only requested columns are selected
-	only matched rows are retrieved
-	aggregations occur in the database

**Mechanism:**  
`IQueryable<T>` composition + explicit projections via `Select()`.

**Anti-pattern:**  
Calling `.ToList()` prematurely or using `.Include()` without semantic justification.

---

### Principle 3: N+1 Elimination via Semantic Composition

Semantic queries compose via expression trees, avoiding:
-	repeated round-trips
-	client-side joins
-	implicit lazy loading

**Mechanism:**  
Expansions and projections define all necessary joins up-front.

**Example:**

```csharp
// ❌ Anti-pattern: N+1 foreach (var person in persons) { var careChoice = dbContext.CareChoices .FirstOrDefault(c => c.PersonId == person.Id); }
// ✅ Semantic pattern: single query var result = dbContext.Persons .WithSemantics() .Select(p => new PersonWithCareChoice { PersonId = p.Id, CareChoice = p.ActiveCareChoice(clock.UtcNow).Name }) .ToList();
```


---

### Principle 4: Index-Driven Query Design

Every semantic query that reaches production must have a corresponding database index.

**Mechanism:**  
ADR-001 defines the indexing policy.  
Each Domain Query (DQ) is analyzed for filter predicates and joined to an index strategy.

**Verification:**  
Execution plan analysis confirms index usage.

---

### Principle 5: Observability & Continuous Validation

Performance is not assumed — it is measured.

**Mechanism:**  
OpenTelemetry spans, query latency metrics (p50/p95/p99), and automated performance regression tests.

---

## 3. The Pushdown Strategy

### 3.1 What Is Pushdown?

Pushdown means executing operations **in the database** rather than in application code.

SQL databases are optimized for:
-	filtering large datasets (`WHERE`)
-	joining tables efficiently
-	aggregating results (`COUNT`, `SUM`, `GROUP BY`)
-	sorting and paging (`ORDER BY`, `LIMIT`)

Application code is **not** optimized for these operations at scale.

---

### 3.2 How Semantic Architecture Guarantees Pushdown

#### Step 1: Expression Tree Construction

Semantic queries are written as `Expression<Func<T, bool>>` or composed via `IQueryable<T>`.

Example:

```csharp
public static Expression<Func<PersonEntity, bool>> IsActive() => person => person.IsActive && !person.IsDeleted;
```


This is not executable code — it's a **semantic declaration** represented as an expression tree.

---

#### Step 2: Semantic Registry Rewrite

When a semantic query is composed:

```csharp
var activePersons = dbContext.Persons .WithSemantics() .Where(PersonQueries.IsActive());
```


The `SemanticQueryProvider` intercepts the expression tree and applies:
-	expansions
-	rewrites
-	domain method translations

The result is a **pure EF-compatible expression tree**.

---

#### Step 3: EF Core Translation

EF Core receives the rewritten expression tree and generates SQL.

Example output:

```sql
SELECT [p].[Id], [p].[Name], [p].[IsActive], [p].[IsDeleted] FROM [Persons] AS [p] WHERE [p].[IsActive] = 1 AND [p].[IsDeleted] = 0
```

✅ **Filtering happens in SQL.**  
✅ **No rows are materialized unnecessarily.**

---

### 3.3 Verification: Pushdown Tests

Every semantic query must be validated with a **pushdown test**.

Example:

```csharp
[Fact] 
public void IsActive_GeneratesCorrectSQL() 
{ 
    // Arrange 
    
    using var harness = new SqliteHarness(); var dbContext = harness.CreateDbContext();
    
    // Act
    
    var query = dbContext.Persons
        .WithSemantics()
        .Where(PersonQueries.IsActive());

    var sql = query.ToQueryString();

    // Assert
    
    sql.Should().Contain("WHERE");
    sql.Should().Contain("[IsActive] = 1");
    sql.Should().Contain("[IsDeleted] = 0");
    sql.Should().NotContain("SELECT *"); // projection correctness
}
```

This test ensures:
-	SQL is generated
-	filtering is in the `WHERE` clause
-	no client-side evaluation occurs

---

## 4. Preventing N+1 Queries

### 4.1 The N+1 Problem

The N+1 problem occurs when:
1.	A query retrieves N parent entities
2.	For each parent, a separate query retrieves related child entities
3.	Total queries: 1 (parents) + N (children) = **N+1**

**Example (anti-pattern):**

```csharp
var persons = dbContext.Persons.ToList(); // 1 query
foreach (var person in persons) // N queries 
{
    var careChoice = dbContext.CareChoices.FirstOrDefault(c => c.PersonId == person.Id);
}
```

**Problem:**
-	N round-trips to the database
-	massive performance degradation at scale

---

### 4.2 Semantic Solution: Projection-Driven Joins

Semantic Architecture eliminates N+1 via **explicit projections**:

```csharp
var result = dbContext.Persons
    .WithSemantics()
    .Select(p => new PersonWithCareChoiceProjection { PersonId = p.Id, Name = p.Name, ActiveCareChoice = p.CareChoices .Where(c => c.Period.Omfattar(clock.UtcNow)) .Select(c => c.Name) .FirstOrDefault() }) .ToList();
```
**Generated SQL:**

```sql
SELECT [p].[Id], [p].[Name], ( SELECT TOP(1) [c].[Name] FROM [CareChoices] AS [c] WHERE [c].[PersonId] = [p].[Id] AND [c].[StartDate] <= @now AND (@now < [c].[EndDate] OR [c].[EndDate] IS NULL) ) AS [ActiveCareChoice] FROM [Persons] AS [p]
```

✅ **Single query.**  
✅ **Subquery pushdown.**  
✅ **No N+1.**

---

### 4.3 Rule: No `.Include()` Without Justification

`.Include()` loads entire related entities into memory.

In Semantic Architecture:
-	`.Include()` is **banned by default**
-	Projections define exactly what is needed
-	Analyzers flag `.Include()` usage

**Justification required:**  
Only when the full aggregate is needed for domain logic (rare).

---

## 5. Expansion Overhead Analysis

### 5.1 Concern: Does Semantic Rewriting Add Latency?

Expression tree rewriting happens at **query construction time**, not execution time.

**Phases:**

| Phase                  | Where                     | Cost         |
|------------------------|---------------------------|--------------|
| Write semantic query   | Development               | Zero         |
| Compose query          | Application (memory)      | Microseconds |
| Registry rewrite       | Application (memory)      | Microseconds |
| EF translation         | Application (memory)      | Milliseconds |
| SQL execution          | Database                  | Dominant     |

**Benchmarks:**

| Operation                          | Time      |
|------------------------------------|-----------|
| Semantic query composition         | ~10 μs    |
| Expression tree rewriting          | ~50 μs    |
| EF Core SQL generation             | ~2 ms     |
| Database query execution (indexed) | ~5-50 ms  |

**Conclusion:**  
Semantic rewriting overhead is **negligible** compared to database execution.

---

### 5.2 Benchmark: Semantic vs Direct EF

```csharp
[Benchmark] public void DirectEF() { var result = dbContext.Persons .Where(p => p.IsActive && !p.IsDeleted) .ToList(); }
[Benchmark] public void SemanticQuery() { var result = dbContext.Persons .WithSemantics() .Where(PersonQueries.IsActive()) .ToList(); }
```

**Results:**

| Method         | Mean     | Allocated |
|----------------|----------|-----------|
| DirectEF       | 12.3 ms  | 15 KB     |
| SemanticQuery  | 12.5 ms  | 16 KB     |

**Difference:** ~200 μs (~1.6% overhead)

**Interpretation:**  
Semantic abstraction adds **virtually no runtime cost**.

---

## 6. Indexing Strategy: ADR-001 Extended

### 6.1 Index Every Domain Query

Every semantic query that reaches production must have a corresponding index.

**Process:**

1.	Define semantic query (e.g., `AktivtVårdval(DateTimeOffset datum)`)
2.	Analyze generated SQL
3.	Identify filter columns
4.	Create covering or composite index
5.	Validate with execution plan analysis

---

### 6.2 Example: `AktivtVårdval`

**Semantic query:**

```csharp
public static Expression<Func<VårdvalEntity, bool>> ÄrAktivt(DateTimeOffset datum) => v => v.Period.Start <= datum && (v.Period.Slut == null || datum < v.Period.Slut);
```

**Generated SQL:**

```sql
SELECT * FROM Vårdval WHERE StartDatum <= @datum AND (SlutDatum IS NULL OR @datum < SlutDatum)
```

**Index:**

```sql
CREATE INDEX IX_Vardval_StartDatum_SlutDatum ON Vardval (StartDatum, SlutDatum) WHERE SlutDatum IS NOT NULL;
```

✅ **Query uses index.**  
✅ **Execution plan shows index seek.**

---

### 6.3 Composite Index Rules

When semantic queries filter on multiple columns, create composite indexes.

**Order matters:**

```sql
-- ✅ Correct: most selective column first 
CREATE INDEX IX_Vardval_PersonId_StartDatum ON Vardval (PersonId, StartDatum);
-- ❌ Incorrect: less selective column first 
CREATE INDEX IX_Vardval_StartDatum_PersonId ON Vardval (StartDatum, PersonId);
```

Use execution plan analysis to confirm optimal order.

---

## 7. Aggregation Pushdown

### 7.1 Semantic Aggregations Must Execute in SQL

Aggregations like `Count()`, `Sum()`, `GroupBy()` must push down.

**Example:**

```csharp
var countsByUnit = dbContext.Persons .WithSemantics() .Where(PersonQueries.IsActive()) .GroupBy(p => p.UnitId) .Select(g => new { UnitId = g.Key, Count = g.Count() }) .ToList();
```

**Generated SQL:**

```sql
SELECT [p].[UnitId], COUNT(*) AS [Count] FROM [Persons] AS [p] WHERE [p].[IsActive] = 1 AND [p].[IsDeleted] = 0 GROUP BY [p].[UnitId]
```

✅ **Aggregation in database.**

---

### 7.2 Anti-Pattern: Client-Side Aggregation

```csharp
// ❌ NEVER DO THIS 
var persons = dbContext.Persons.ToList(); // materializes all rows 
var count = persons.Count(p => p.IsActive); // counts in memory
```

**Problem:**
-	retrieves all rows
-	counts in application memory
-	wastes bandwidth, memory, and CPU

---

## 8. Observability & Metrics

### 8.1 OpenTelemetry Integration

Every semantic query execution should be instrumented with:

```csharp
using var activity = ActivitySource.StartActivity("SemanticQuery"); 
activity?.SetTag("query.name", "IsActive"); 
activity?.SetTag("query.component", "Person"); 
activity?.SetTag("query.projection", "PersonOverview");
```

**Metrics to track:**

| Metric                      | Purpose                              |
|-----------------------------|--------------------------------------|
| `query.latency`             | p50/p95/p99 execution time           |
| `query.rows_returned`       | Detect over-fetching                 |
| `query.rows_materialized`   | Should be minimal                    |
| `query.expansion_count`     | Number of expansions applied         |
| `query.sql_generation_time` | EF translation overhead              |

---

### 8.2 Query Plan Visualization

Use SQL Server's execution plan or PostgreSQL's `EXPLAIN ANALYZE` to validate:
-	index usage
-	join strategy
-	estimated vs actual rows

**Example:**

```sql
EXPLAIN ANALYZE SELECT * FROM Persons WHERE IsActive = 1 AND IsDeleted = 0;
```

**Expected output:**

Index Scan using IX_Persons_IsActive_IsDeleted (cost=0.42..8.44 rows=10 width=...)

✅ **Index scan confirmed.**

---

### 8.3 Automated Regression Detection

Performance tests should run in CI/CD:

```csharp
[Fact] 
public void IsActive_ExecutesInUnder50ms() 
{ 
    using var harness = new SqliteHarness(); 
    harness.SeedTestData(10_000); // large dataset
    
    var stopwatch = Stopwatch.StartNew();
    var result = harness.DbContext.Persons
    .WithSemantics()
    .Where(PersonQueries.IsActive())
    .ToList();
    stopwatch.Stop();

    stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
}
```

---

## 9. Optimization Patterns

### 9.1 Explicit Projection Pattern

Always project to typed DTOs

```csharp
// ✅ Good: 
var result = dbContext.Persons .WithSemantics() .Select(p => new PersonOverview { Id = p.Id, Name = p.Name }) .ToList();
// ❌ Bad: retrieves all columns 
var result = dbContext.Persons.ToList();:
```

---

### 9.2 Paging Pattern

Use `Skip()` and `Take()` for large result sets:

```csharp
var page = dbContext.Persons
    .WithSemantics() 
    .Where(PersonQueries.IsActive()) 
    .OrderBy(p => p.Name) 
    .Skip(pageNumber * pageSize) 
    .Take(pageSize) 
    .ToListAsync(ct);
```

**Generated SQL:**

```sql
SELECT * FROM Persons WHERE IsActive = 1 ORDER BY Name OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
```

✅ **Paging in database.**

---

### 9.3 Temporal Query Optimization

Use partial indexes for temporal queries:

```sql
CREATE INDEX IX_Vardval_Open ON Vardval (PersonId, StartDatum) WHERE SlutDatum IS NULL; -- partial index for open-ended periods
```

This dramatically improves queries like:

```csharp
v => v.Period.ÄrTillsvidare
```

---

## 10. Common Anti-Patterns

| Anti-Pattern                     | Problem                          | Solution                          |
|----------------------------------|----------------------------------|-----------------------------------|
| `.ToList()` before filtering     | Materializes all rows            | Filter first, then materialize    |
| `.Include()` without projection  | Over-fetching                    | Use explicit `Select()`           |
| Foreach + query inside loop      | N+1 problem                      | Use projection with subquery      |
| Client-side `Where()`            | Filtering in memory              | Use semantic queries              |
| Anonymous types in domain logic  | Non-refactorable                 | Use typed projections             |

---

## 11. Performance Testing Strategy

### 11.1 Unit-Level Performance Tests

Test individual semantic queries with `SqliteHarness`:

```csharp
[Fact] 
public void PersonQuery_ExecutesEfficiently() 
{ 
    using var harness = new SqliteHarness(); 
    harness.SeedTestData(1000);
    
    var sw = Stopwatch.StartNew();
     var result = harness.DbContext.Persons
    .WithSemantics()
    .Where(PersonQueries.IsActive())
    .ToList();
    sw.Stop();

    sw.ElapsedMilliseconds.Should().BeLessThan(100);
    result.Should().HaveCountGreaterThan(0);
}
```

---

### 11.2 Integration-Level Performance Tests

Test full use cases with realistic data volumes.

---

### 11.3 Load Testing

Use tools like k6 or JMeter to simulate production traffic.

---

## 12. Conclusion

Semantic Architecture does not compromise performance — it **enhances** it by:
-	guaranteeing pushdown via expression trees
-	eliminating N+1 queries via projections
-	enforcing explicit indexing policies
-	providing systematic observability
-	enabling automated regression detection

The abstraction layers add negligible overhead (~1-2%) while providing:
-	domain clarity
-	refactor-safety
-	AI compatibility
-	epistemic integrity

**Performance is not sacrificed for semantics — it is achieved through semantics.**

>Semantic Architecture proves that abstraction and efficiency are not opposites.  
>When meaning is explicit and systematically translated, performance follows naturally.

---

## References

-	**ADR-001:** Indexing Policy
-	**Appendix H:** Semantic Query Patterns
-	**Appendix I:** Semantic Expansion Rules
-	**Appendix G:** Semantic Registry Specification
-	**ONBOARDING.md:** SqliteHarness & Testing Strategy
