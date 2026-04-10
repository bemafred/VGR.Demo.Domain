# Appendix C: Semantic Components

***A Formal Definition of Components in Semantic Architecture***

> **Epistemisk status: Emergence**
> Semantic Components som koncept är inte formaliserat i koden genom dedikerade attribut
> eller registrering. Domäntyper klassificeras via `DomainTypeKind` (Aggregate, Entity,
> ValueObject etc.) i `DomainModel`, inte via en explicit komponentmodell.
> Dokumentet beskriver en konceptuell målbild.

## 1. Introduction

The term component is widely used across the software industry, often with inconsistent or ambiguous meaning.
In Semantic Architecture, the concept is defined precisely and serves as a foundational unit of modularity, semantics, and epistemic clarity.

A Semantic Component is not a microservice, module, class, or deployment boundary.
It is a semantic unit: a cohesive, self-contained expression of domain meaning, governed by explicit invariants and expressed through semantic queries, projections, and commands.

This appendix defines Semantic Components, their required properties, and how they interact within E-Clean & Semantic Architecture.

---

## 2. Definition

A Semantic Component is:

>A self-describing, domain-aligned, invariant-preserving unit of meaning that exposes behaviour and knowledge through semantic expressions rather than technical APIs.
A Semantic Component is defined by meaning, not infrastructure.

---

## 3. Characteristics of a Semantic Component

### 3.1 Domain-Centric

A component embodies a coherent domain concept, such as:
-	Care Choice (Vårdval)
-	Patient Identity
-	Compensation Rule
-	Healthcare Unit
-	Time Interval
-	Outcome Classification

It represents a natural concept that exists independently of the system’s architecture.

### 3.2 Invariant-Preserving

Each component must enforce its own invariants:
-	temporal relations
-	identity rules
-	business constraints
-	domain behaviours

These invariants must reside in the domain, not in controllers, EF configurations, services, or handlers.

### 3.3 Semantically Expressive

Components expose their meaning through:
-	Semantic queries (expression-based LINQ)
-	Domain projections (typed, navigable representations)
-	Expansion rules (cross-component knowledge integration)
-	Domain methods (explicit invariants and behaviour)

This allows other parts of the system to consume the meaning, not the implementation.

### 3.4 Refactorable Contracts

A Semantic Component exposes projections that are:
-	typed
-	refactor-safe
-	IDE-navigable
-	analyzable by AI and Roslyn
-	non-serialised by default (they reflect knowledge, not transport)

Contracts evolve through refactoring and semantic versioning, not unstable REST schemas or hand-maintained DTOs.

### 3.5 Infrastructure-Agnostic

A component must not depend on:
-	EF DbContexts
-	HTTP APIs
-	message brokers
-	external persistence
-	serialization formats
-	networking

These concerns belong to the semantic execution and application layers, not to the domain of a component.

### 3.6 Composable

A Semantic Component can be composed with others through:
-	expansions
-	combined projections
-	expression tree composition
-	semantic transformations
-	orchestration in the Application layer

Components combine through meaning, not through hardcoded dependencies.

### 3.7 Testable in Isolation

A component can be verified using:
-	invariant tests
-	projection tests
-	semantic expansion tests

…without requiring:
-	databases
-	HTTP
-	mocks
-	infrastructure
-	inter-services calls

The component is self-contained.

---

## 4. Boundaries of a Semantic Component

A Semantic Component typically includes:
1.	Domain Types
-	Value objects
-	Aggregates
-	Domain exceptions (or results)
-	Domain vocabulary (names, concepts, events)
2.	Semantic Definitions
-	SemanticQuery methods
-	Expansion rules
-	Expression trees
-	Navigation logic
3.	Projections
-	Typed records/classes representing knowledge views
-	Domain-aligned subsets of information
-	Used by Application and Infrastructure layers

A component does not include:
-	repositories
-	DTOs
-	controllers
-	service classes
-	serialized contract definitions
-	transport- or persistence-specific logic

---

## 5. Component Interactions

Components interact through:

### 5.1 Semantic Queries

One component may express questions about another component’s domain:

```csharp
TODO: Fix Example like Semantics.For<Person>().ActiveVardval(DateTimeOffset now)
```

This is not a service call; it is a semantic query defined in code.

### 5.2 Expansion Rules

Expansion rules allow automatic inclusion of cross-component relations, such as:
-	“A Person has an active Care Choice”
-	“A Compensation Rule applies to this Time Interval”
-	“A Healthcare Unit belongs to this region”

These expansions can be discovered and composed automatically.

### 5.3 Projections

Components exchange information through typed projections, not through:
-	JSON
-	RPC
-	raw entities
-	DTO chains

Projections represent knowledge, not data transfer.

### 5.4 Orchestration

The Application layer orchestrates component interactions by combining semantic queries, domain commands, and execution pipelines.

Components remain independent.

---

## 6. Component Lifecycle

A Semantic Component evolves through:
-	domain refinement
-	updated invariants
-	new semantic queries
-	new projections
-	refactoring of relationships
-	improvements in domain vocabulary

Since components are semantic and not infrastructural:
-	they are versioned through refactoring
-	they remain stable through conceptual coherence
-	they do not require API-breaking changes
-	they do not depend on transport schemas

This makes Semantic Components ideal for long-lived, mission-critical systems (e.g., healthcare, compensation, identity, case management).

---

## 7. Comparison with Conventional Component Models

Traditional Components
-	Defined by deployment boundaries
-	Coupled to frameworks
-	Exposed via APIs
-	DTO-driven
-	Fragile under refactoring
-	Often anaemic

Microservices
-	Deployment-oriented
-	Modeled around system decomposition
-	High infrastructural overhead
-	Difficult to evolve without coordination

Semantic Components
-	Defined by meaning
-	Domain-aligned
-	Self-describing
-	Runtime-composable
-	Infrastructure-agnostic
-	Refactor-safe
-	AI-readable

>Semantic Components are domain artefacts, not deployment units.

---

## 8. Conclusion

Semantic Components represent a radical shift in how modularity is conceptualized in .NET systems.

They are:
-	meaning-first
-	domain-driven
-	invariant-preserving
-	tooling-friendly
-	runtime-composable
-	architecturally independent
-	AI-native

A Semantic Component is not what the system does.
It is what the system means.

They form the structural backbone of E-Clean & Semantic Architecture and enable the creation of systems that are:
-	navigable
-	scalable
-	maintainable
-	epistemically clean
-	semantically expressive
-	suitable for AI augmentation (e.g., Sky Omega)

