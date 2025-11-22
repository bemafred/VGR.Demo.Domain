# AI-GUIDANCE for E-Clean & Semantic Architecture

This repository uses **Epistemic Clean (E-Clean)** and **Semantic Architecture**.  
The core idea: **the Domain is the truth**. Semantics and projections exist to make the Domain queryable and usable – not to replace it.

This document describes how AI tools (Copilot, Rider AI, ChatGPT, etc.) should behave when generating or editing code.

---

## 1. Domain is the source of truth

The Domain (`VGR.Domain`) defines:

- Concepts (aggregates, value objects)
- Invariants
- Ubiquitous language
- Semantic annotations (attributes)

**AI MUST:**

- Prefer extending the Domain when a new concept is needed.
- Never encode new “truth” only in queries, projections or infrastructure.
- Use factory methods / `Tolka` / value object semantics instead of bypassing invariants.

If something feels like “business meaning”, it belongs in the Domain.

---

## 2. Semantics.Linq – capability to handle semantic queries

`VGR.Semantics.Linq` exists to provide the **ability to handle semantic queries** over the Domain.

It:

- Exposes *semantic query operators* over domain types.
- Uses expressions (`Expression<Func<...>>`) or similar to describe queries.
- Bridges Domain concepts to the underlying data model in a **declarative**, semantic way.

**AI MUST:**

- Place new semantic query capabilities here when they express *how we can query* existing domain concepts.
- Keep implementations expression-based, suitable for EF translation and composition.
- Never introduce business rules here that are missing in the Domain – instead, ask for the Domain to be extended.

Think of `Semantics.Linq` as **“how we can ask questions about the truth”**, not as an alternative truth.

---

## 3. Domain.Queries – projections and views only

`Domain.Queries` is the home for:

- Projections
- Read models
- View-oriented shapes (DTOs, records used by API/UI/reporting)
- Composition of existing semantic query capabilities into *use-case oriented* results

**AI MUST:**

- Place projections and view-specific DTOs here.
- Avoid embedding EF details or infrastructure concerns in `Domain.Queries`.
- Use existing semantic query operators from `VGR.Semantics.Linq` when building projections.

**AI MUST NOT:**

- Invent new semantics here.
- Hide invariants or business logic in projections.
- Use `Domain.Queries` as a shortcut to avoid extending the Domain.

`Domain.Queries` answers:
> “Given the truth (Domain) and how we can query it (Semantics.Linq), what shape do we want to present to callers?”

---

## 4. Preferred direction of change

When AI suggests changes, the order of preference is:

1. **Extend the Domain** when a concept is missing.
2. **Add or extend semantic query capabilities** in `VGR.Semantics.Linq` to express how to query those concepts.
3. **Add or extend projections/views** in `Domain.Queries` to deliver the shapes needed by API/UI/use-cases.
4. Only then touch Infrastructure (EF, transports, etc.) to wire things up.

Never introduce a new business concept only at level 2–4.

---

## 5. E-Clean: keep epistemic and technical concerns separated

**AI MUST:**

- Keep Domain and Semantics free from:
    - logging
    - HTTP details
    - persistence plumbing
    - transport-specific concerns
- Keep EF mapping, DbContexts etc. in Infrastructure projects.
- Use semantic capabilities from `VGR.Semantics.Linqq` rather than writing raw EF queries in Application/Infrastructure when domain semantics already exist.

If a suggestion “knows” about tables, HTTP headers or transport IDs – it likely does **not** belong in Domain or Semantics.

---

## 6. Code ergonomics and naming

This codebase optimizes for **kodergonomi**:

- Readability and IntelliSense navigation
- Clear, domain-driven naming
- Minimal abstractions
- C# 14 / .NET 10 idioms where appropriate

**AI SHOULD:**

- Use meaningful, domain-aligned names in Swedish where the domain is Swedish.
- Prefer:
    - `nameof(...)` over magic strings
    - expression-bodied members where they improve clarity
    - small, composable methods over deep nested logic
- Avoid over-engineering, generic “frameworks” and unnecessary indirection.

When in doubt: prefer the **simpler**, more **explicit** solution.

---

## 7. Handling Value Objects and invariants

For Value Objects:

- Creation goes through factory methods / `Tolka` / dedicated constructors.
- Invariants are checked at the edges.

**AI MUST:**

- Not create “shortcut” constructors that bypass validation.
- Not introduce setters or mutable state to simplify mapping.
- Use explicit mapping helpers (in Infrastructure) rather than weakening the VO design.

---

## 8. Generator interaction

Some semantics/registrations are generator-driven.

**AI SHOULD:**

- Avoid modifying generated files.
- Propose changes in:
    - Domain (new attributes, concepts)
    - Template-friendly structures
    - Semantic registrations (`SemanticRegistry`, etc.)

If a feature feels “magic”, look for the source in Domain + generator + semantics – don’t duplicate it.

---

## 9. Questions AI should ask instead of guessing

When the assistant is uncertain, it should prefer questions like:

- “Does this belong in Domain (truth), Semantics.Queries (query capability), or Domain.Queries (projection)?”
- “Is this a new business concept that should be modelled in the Domain first?”
- “Is there an existing semantic operator in `VGR.Semantics.Linq` that should be reused instead of writing raw EF?”

Guessing and placing logic in the wrong layer increases entropy and violates E-Clean.

---

## 10. Testning — SqliteHarness unified

Testinfrastrukturen är centraliserad i `VGR.Technical.Testing.SqliteHarness`.

**AI MUST:**

- Use `SqliteHarness` for any test needing DB access (semantic correlation, E2E).
- NOT duplicate SqliteHarness logic across projects.
- Place domain unit tests (`VGR.Domain.Tests`) WITHOUT SqliteHarness (pure aggregats).
- Place semantic correlation tests (`VGR.Semantics.Linq.CorrelationTests`) WITH SqliteHarness.
- Place E2E tests (`VGR.Tests`) WITH SqliteHarness.

**Example: Semantic correlation test**

```csharp
[Fact]
public async Task MySemanticMethod_MatchesSql()
{
    await using var h = new SqliteHarness();
    
    // Domain: in-memory
    var result = myObject.SemanticMethod();
    
    // SQL: via WithSemantics() rewriting
    var sqlResult = await h.Read.MyDbSet
        .WithSemantics()
        .Where(x => x.SemanticMethod())
        .AnyAsync();
    
    Assert.Equal(result, sqlResult);
}
```

**Example: E2E test**

```csharp
[Fact]
public async Task CreatePerson_End2End()
{
    await using var h = new SqliteHarness();
    var interactor = new SkapaPersonInteractor(h.Read, h.Write, clock);
    
    var result = await interactor.ProcessAsync(cmd, ct);
    
    // Verify persistence
    var saved = await h.Read.Persons.FirstOrDefaultAsync(p => p.Id == result.Value);
    Assert.NotNull(saved);
}
```



By following these guidelines, AI assistants will reinforce – not erode – the principles behind **E-Clean** and **Semantic Architecture** in this solution.