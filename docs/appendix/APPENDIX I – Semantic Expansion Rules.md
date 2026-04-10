# Appendix I: Semantic Expansion Rules

***Formal Specification for Cross-Component Semantic Enrichment in Semantic Architecture***

> **Epistemisk status: Delvis Engineered**
> Expansionsmekanismen är implementerad: `[ExpansionFor]`-attribut (tar två parametrar:
> `targetType` och `targetMethodName`), `SemanticRegistry` med iterativ expression
> rewriting (upp till 8 djup). Följande delar är ännu inte implementerade:
> prioritetslogik (§6.2), kategorisering av expansionstyper (§5), komponentgränshantering
> (§7), villkorad tillämpning. Alla expansioner appliceras likvärdigt utan prioritetsordning.

## 1. Introduction

Semantic Expansion Rules (“expansions”) are one of the most powerful mechanisms in Semantic Architecture.

They enable:
-	cross-component relationships
-	enriched semantic queries
-	domain navigation without technical coupling
-	modular composition of meaning
-	dynamic binding of additional logic

Expansions turn the architecture into a **semantic graph**, where meaning flows between components based on explicitly declared relationships.

Unlike service calls, repositories, or ad-hoc joins, expansions are ***semantic declarations*** of how components are related conceptually.

This appendix defines the structure, behavior, constraints, and lifecycle of Semantic Expansion Rules.

---

## 2. Purpose of Expansion Rules

Expansion rules exist to:
1.	Connect semantic components through meaning, not through infrastructure.
2.	Enrich queries with additional context.
3.	Centralize cross-domain knowledge in a discoverable, analyzable form.
4.	Enable modular growth without modifying existing components.
5.	Support projection-driven execution in EF Core.
6.	Expose domain relations to AI tooling for navigation and synthesis.

In short:

>Expansions are declarations of meaning that augment semantic execution.

---

## 3. Definition of an Expansion Rule

A Semantic Expansion Rule is:

>An expression-based semantic transformer that links one component or entity to another by enriching meaning during query construction.

This rule must:
-	be deterministic
-	be EF-translatable
-	be analyzable
-	be discoverable via attributes
-	respect component boundaries
-	express domain semantics, not technical operations

---

## 4. Structure of an Expansion Rule

### 4.1. Attribute-Based Declaration

Expansions are declared using an attribute with two parameters:

```csharp
[ExpansionFor(typeof(PersonEntity), nameof(PersonEntity.ActiveCareChoice))]
```

This indicates:
-	the target type
-	the target method name (the `[SemanticQuery]`-marked method being expanded)

### 4.2. Expansion Method Signature

An expansion must be represented as:

```csharp
public static Expression<Func<TSource, TExpanded>> ExpansionName(params)
```

or:

```csharp
public static IQueryable<TProjection> Expand(this IQueryable<TSource> source, params)
```

### 4.3. Components Registered

The expansion method updates:
-	the Expansion Registry
-	the Component Registry
-	semantic dependencies
-	projection maps

### 4.4. Expression Shape

Expansion expressions must be:
-	pure
-	referentially transparent
-	devoid of side effects
-	EF-compatible
-	typed

---

## 5. Types of Expansion Rules

Semantic Architecture supports several categories of expansions.

---

### 5.1 Direct Relationship Expansions

Express a one-to-one or one-to-many domain relation.

#### Example:

```csharp
[ExpansionFor(typeof(PersonEntity))]
public static Expression<Func<PersonEntity, IEnumerable<CareChoiceEntity>>> CareChoices()
    => p => p.CareChoices.Where(c => !c.IsDeleted);
```

#### Used for:
-	navigation
-	projections
-	domain reasoning

---

### 5.2 Temporal Expansions

Add meaning that depends on time.

#### Example:

```csharp
[ExpansionFor(typeof(PersonEntity))]
public static Expression<Func<PersonEntity, CareChoiceEntity?>> ActiveCareChoice(DateOnly datum)
    => p => p.CareChoices.FirstOrDefault(c => c.Period.Omfattar(datum));
```

#### Used for:
-	healthcare periods
-	compensation validity
-	eligibility rules

---

### 5.3 Cross-Component Expansions

Link components without hard coupling.

#### Example:

```csharp
[ExpansionFor(typeof(PersonEntity))]
public static Expression<Func<PersonEntity, UnitEntity?>> HomeUnit()
    => p => p.Unit;
```

#### Allows:
-	semantic component stitching
-	domain graph navigation
-	multi-component projections

---

### 5.4 Computed Expansions

Introduce derived meaning not stored physically.

#### Example:

```csharp
[ExpansionFor(typeof(PersonEntity))]
public static Expression<Func<PersonEntity, bool>> IsEligible()
    => p => p.Age >= 18 && p.ActiveCareChoice != null;
```

#### Used for:
-	rule engines
-	decision support
-	business logic

---

### 5.5 Projection-Driven Expansions

Produce projections for downstream composition.

#### Example:

```csharp
[ExpansionFor(typeof(PersonEntity))]
public static Expression<Func<PersonEntity, PersonOverview>> ToOverview()
    => p => new PersonOverview
    {
        PersonId = p.Id,
        Name = p.Name,
        ActiveCareChoice = p.ActiveCareChoice.Name
    };
```

These expansions unify queries across layers.

---

## 6. Expansion Composition Rules

Expansions must follow deterministic composition rules.

---

### 6.1 Automatic Application

When composing semantic queries, the Semantic Registry automatically applies all applicable expansions.

This ensures:
-	cross-domain meaning is always present
-	queries remain coherent
-	business rules are not accidentally omitted

---

### 6.2 Priority

Some expansions may override or extend others.

Priority rules ensure:
-	temporal expansions outrank static ones
-	component-local expansions outrank cross-component expansions
-	narrower expansions outrank general ones

---

### 6.3 Purity & EF Compatibility

Expansions must be:
-	side-effect-free
-	deterministic
-	expression-only

They must avoid:
-	imperative logic
-	loops
-	I/O operations
-	external services

---

### 6.4 Component Boundary Enforcement

Expansions define semantic relations, not arbitrary coupling.

An expansion declaring:

```csharp
[ExpansionFor(typeof(PersonEntity))]
```

must only reference:
-	domain types
-	projections
-	related components via meaning

It may not reference:
-	command handlers
-	controllers
-	services
-	infrastructure

---

## 7. Registry Integration

Expansions are stored in the **Expansion Registry**, accessible through the Semantic Registry.

The registry maintains:
-	source → target type relations
-	expansion names
-	expansion priorities
-	expression AST representations
-	symbolic relationships
-	projection compatibility tables

This supports:
-	EF translation
-	AI reasoning
-	tooling-based safety checks

---

## 8. AI Participation

Expansions are ideal AI reasoning units because they are:
-	typed
-	deterministic
-	semantic
-	discoverable
-	safely composable

AI systems (Sky, James, Lucy) can use expansions to:
-	reason about component interaction
-	synthesize new queries
-	validate semantic coverage
-	propose new expansions
-	detect missing rules
-	generate projection code

Expansions effectively convert the system to a **semantic graph representation** accessible to both humans and machines.

---

## 9. Failure Modes & Mitigations

### 9.1 Expansion Conflicts

Mitigated by priority rules and semantic validation.

### 9.2 Untranslatable Expressions

Mitigated by analyzers + EF compatibility checks.

### 9.3 Cyclic Expansions

Mitigated by registry cycle detection.

### 9.4 Hidden Coupling

Mitigated by component boundary enforcement.

---

## 10. Conclusion

Semantic Expansion Rules turn the architecture into a dynamic semantic system, enabling:
-	cross-domain meaning
-	modular evolution
-	safe composition
-	projection-centric execution
-	AI-assisted reasoning
-	epistemic clarity across components

They allow .NET systems to act as if they were:
-	semantically aware
-	graph-structured
-	domain-coherent

…without requiring databases, infrastructure, or runtime frameworks beyond standard .NET.

>Expansions are the mechanism through which the codebase becomes a semantic network.
They are the connective tissue of Semantic Architecture.

