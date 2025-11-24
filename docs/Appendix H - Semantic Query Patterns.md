# Appendix H: Semantic Query Patterns

***Formal Patterns for Expressing Meaning Through LINQ and Expression Trees in Semantic Architecture***

## 1. Introduction

Semantic queries are a core mechanism in Semantic Architecture.
They allow domain meaning—rules, invariants, relationships, and constraints—to be expressed in executable form using:
-	expression trees
-	typed projections
-	semantic expansions
-	LINQ-based composition

This appendix defines the primary patterns used to construct semantic queries, enabling:
-	predictable behaviour
-	safe composability
-	projection correctness
-	refactorability
-	automated validation
-	AI-assisted synthesis

Each pattern is necessary to preserve semantic clarity and epistemic integrity.

---

## 2. Role of Semantic Queries

Semantic queries serve as:
1.	Domain-aware knowledge accessors
2.	Composable meaning units
3.	Inputs to EF Core’s semantic execution pipeline
4.	Anchors for projections
5.	Documentation of domain intent
6.	Machine-readable semantic objects for AI

A semantic query is not a data access operation — it is an ***expression of meaning*** that the platform can transform, expand, and inspect.

---

### 3. Core Patterns

Semantic queries follow one or more of the following canonical patterns.

---

#### Pattern 1: Predicate Queries

##### Description

A predicate query expresses a boolean rule over a domain entity.

##### Example

```csharp
public static Expression<Func<PersonEntity, bool>> IsActive()
    => person => person.IsActive && !person.IsDeleted;
```

Uses
-	Filters
-	Invariant checks
-	Projection entry points
-	Expansion anchors

Guarantees
-	Composable via AndAlso, OrElse
-	Always safe for EF translation
-	Machine-inspectable

---

### Pattern 2: Projection Queries

#### Description

A semantic query that maps from an entity to a typed projection.

#### Example

```csharp
[SemanticQuery(typeof(PersonOverview))]
public static IQueryable<PersonOverview> Overview(this IQueryable<PersonEntity> source)
{
    return source.Select(p => new PersonOverview
    {
        Id = p.Id,
        Name = p.Name,
        Status = p.IsActive ? "Active" : "Inactive"
    });
}
```

Uses
-	Application-level read models
-	Domain knowledge surfaces
-	Inter-component communication

Guarantees
-	Projection types are refactor-safe
-	Output is semantically meaningful
-	EF- and tooling-friendly

---

### Pattern 3: Composite Queries

#### Description

Queries that combine multiple predicates, projections, or expansions.

#### Example

```csharp
public static IQueryable<PersonOverview> ActivePersons(
    this IQueryable<Person> personer,
    DateTimeOffset now)
{
    return personer
        .Where(person => person.IsActive)
        .Where(person => person.HasValidCareChoice(now))
        .Select(ProjectToOverview());
}
```

Behaviour
-	Composed entirely from semantic fragments
-	Automatically applies expansions
-	Produces deterministic, consistent output

---

### attern 4: Expansion-Based Queries

#### Description

Queries enriched with automatically discovered expansion rules.

#### Example

```csharp
[ExpansionFor(typeof(PersonEntity))] // TODO: Exemplet är olagligt
public static Expression<Func<PersonEntity, CareChoiceEntity?>> ActiveCareChoice(DateTimeOffset now)
{
    return person => person.CareChoices
        .FirstOrDefault(c => c.ValidFrom <= now && now < c.ValidTo);
}
```

Behaviour
-	Integrated automatically during query construction
-	Allows cross-component semantics
-	Ensures meaning is kept out of application layer

---

### Pattern 5: Temporal Queries

#### Description

Queries involving time, validity, or intervals.

#### Example

```csharp
public static Expression<Func<Person, bool>> AktivVid(DateTimeOffset datum)
=> person => person.Period.Innehåller(datum);
```

Uses
-	Healthcare validity windows
-	Compensation periods
-	Lifecycle logic

Guarantees
-	EF-translatable
-	AI-readable
-	Domain-explicit

---

### Pattern 6: Joined Semantic Queries

#### Description

Queries combining two or more components through semantic relations.

#### Example

```csharp
public static IQueryable<PersonCareChoiceProjection> CareChoiceDetails(
    this IQueryable<PersonEntity> persons,
    IQueryable<CareChoiceEntity> careChoices)
{
    return
        from p in persons
        join c in careChoices on p.Id equals c.PersonId
        select new PersonCareChoiceProjection
        {
            PersonId = p.Id,
            CareChoice = c.Name
        };
}
```

Behaviour
-	Captures cross-domain meaning
-	Provides semantic bridge between components
-	Enables joined projections

---

### Pattern 7: Aggregation Queries

#### Description

Queries producing semantic summaries or domain-level aggregates.

#### Example

```csharp
public static IQueryable<PersonCountByUnitProjection> CountByUnit(
    this IQueryable<PersonEntity> source)
{
    return source
        .GroupBy(p => p.UnitId)
        .Select(group => new PersonCountByUnitProjection
        {
            UnitId = group.Key,
            Count = group.Count()
        });
}
```

Uses
-	Statistical queries
-	Operational analytics
-	Decision-support logic

---

## 4. Query Composition Rules

Semantic queries obey the following composition rules:

### 4.1 Purity

Queries must not contain:
-	side effects
-	I/O calls
-	service calls
-	logging
-	exceptions as flow

They express meaning, not behaviour.

### 4.2 Refactorability

All queries must remain:
-	syntactically navigable
-	semantically clear
-	analyzable by Roslyn
-	EF-translatable

### 4.3 Composable Form

Queries must be:
-	expression-based
-	projection-driven
-	expansion-compatible
-	idiomatic LINQ

### 4.4 Component Boundaries

Queries must not violate semantic component boundaries unless:
-	explicitly expanded
-	explicitly joined
-	explicitly projected

---

## 5. Discoverability and Reflection

The Semantic Registry discovers queries via:
-	attribute scanning
-	Roslyn symbol analysis
-	reflection of method signatures
-	inspection of expression trees

Queries must follow patterns that allow this discovery to work robustly.

---

## 6. AI Compatibility

Because semantic queries are:
-	typed
-	deterministic
-	expression-based
-	refactor-safe
-	projection-driven

…AI systems can:
-	generate them
-	validate them
-	refactor them
-	reason about their meaning
-	identify invariants they rely on
-	combine them safely

Semantic Architecture is AI-native precisely because these patterns are enforced.

---

## 7. Failure Modes & Mitigations

### 7.1 EF Non-Translation

Mitigation: enforce expression purity; analyzers check translatability.

### 7.2 Semantic Drift

Mitigation: registry validation and projection typing.

### 7.3 Component Leakage

Mitigation: semantic component boundaries enforced via analyzers.

### 7.4 Missing Projections

Mitigation: registry reconciliation.

---

## 8. Conclusion

Semantic Query Patterns are not optional conventions—they are required primitives for Semantic Architecture.

They ensure that:
-	domain logic is expressed semantically
-	queries remain analyzable
-	projections remain safe
-	expansions remain discoverable
-	AI tooling can participate meaningfully
-	EF Core can execute domain meaning, not just data access

>Semantic queries turn C# code into executable knowledge.
These patterns define the grammar of that knowledge.
