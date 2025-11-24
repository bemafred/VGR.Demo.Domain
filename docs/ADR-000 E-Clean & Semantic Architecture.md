# ADR-000: Epistemic Clean & Semantic Architecture

***Formell deklaration av arkitekturmönster och implementationsprinciper***

---

## Metadata

| Field                  | Value                                      |
|------------------------|--------------------------------------------|
| **Status**             | Accepted                                   |
| **Version**            | 1.0.0                                      |
| **Date**               | 2025-01-24                                 |
| **Decision Makers**    | VGR Architecture Team                      |
| **Supersedes**         | None (foundational)                        |
| **Related ADRs**       | ADR-001 (Index), ADR-002 (Semantic Names)  |

---

## Context

Traditional .NET architectures struggle with several fundamental problems:

1. **Semantic drift** – domain knowledge disperses across layers (controllers, services, repositories, database)
2. **Epistemic fragmentation** – "truth" exists in multiple forms (C# models, SQL schemas, API contracts, documentation)
3. **Translation overhead** – constant mental context-switching between domain language and technical artifacts
4. **AI incompatibility** – codebases lack machine-readable semantic structure
5. **Refactoring brittleness** – domain changes ripple unpredictably through infrastructure
6. **Performance vs clarity trade-off** – optimized queries often sacrifice domain expressiveness

Existing approaches (Clean Architecture, DDD, CQRS) address some concerns but:
- Do not solve the **semantic translation problem** (domain → database)
- Do not provide **machine-readable domain catalogs**
- Do not optimize for **code ergonomics** as a first-class concern
- Do not integrate **AI-assisted development** as a core architectural principle

We needed an architecture that:
- Keeps domain knowledge **pure and central**
- Makes semantics **executable** via systematic translation
- Treats **language as the primary interface**
- Enables **both humans and AI** to reason about the system
- Maintains **production-grade performance** despite abstraction layers

---

## Decision

We adopt **Epistemic Clean Architecture (E-Clean)** as our architectural pattern and **Semantic Architecture** as its concrete implementation.

### What is E-Clean?

**Epistemic Clean Architecture (E-Clean)** is an architectural pattern that:
- Builds on a **pure, rich domain** expressing knowledge and invariants
- Maintains **strict separation** between domain (epistemic) and infrastructure (technical)
- Achieves **code ergonomics** through linguistic expressions rather than technical artifacts
- Uses **systematic semantic translation** from domain language to executable queries

**Tagline:** *Language is the interface. Semantics execute.*

### What is Semantic Architecture?

**Semantic Architecture** is the concrete implementation of E-Clean principles through:

1. **Semantic Registry** – machine-readable catalog of domain concepts, queries, projections, and relationships
2. **Semantic Queries** – expression-based domain knowledge accessors (APPENDIX H)
3. **Semantic Expansions** – declarative cross-component enrichment rules (APPENDIX I)
4. **Expression Tree Rewriting** – translation of domain methods to EF-compatible SQL
5. **Projection-Driven Execution** – typed read models preserving semantic meaning
6. **Domain Catalog** – RDF/Turtle export of domain structure for visualization and AI reasoning

**Key distinction:**
> **E-Clean defines *how* we build. Semantic Architecture defines *what* we build.**

Just as MVC is a pattern and ASP.NET MVC is an implementation.

---

## Architectural Principles

### 1. Pure Domain
Aggregates and value objects express semantics and invariants.  
**No** `IQueryable`, `Expression`, or EF in the domain layer.

### 2. Semantic Persistence
EF translators convert domain APIs (e.g., `Tidsrymd.Överlappar`) to SQL.  
One source of truth for business rules.

### 3. Application as Prose
Interactors express use cases in domain vocabulary, not infrastructure terms.

### 4. Ports & Adapters
Readers/Writers as application abstractions.  
EF/Dapper/views behind ports.

### 5. Domain Catalog
Concepts/Relations/Rules as C# attributes, exported to Turtle.  
`/domain` endpoint visualizes in runtime.

### 6. Explainability-First
Every decision must be traceable: log rules, inputs, intermediate results.

### 7. Simplicity First
Minimal necessary layers.  
Introduce Specification/CQRS only when clearly beneficial.

### 8. Verifiability
Correlation tests between domain predicates (in-memory) and generated SQL (`ToQueryString()`).

### 9. Provider Portability
Translators have provider-specific branches (SqlServer/Npgsql/Sqlite) when needed.

### 10. Observability-Ready
OpenTelemetry (traces/metrics/logs) with semantic tags: `ruleset`, `period`, `job_id`.

---

## Architectural Layers

[ UI / API ]↓ [ Application / Interactors ] —— calls ——> [ Ports (Readers/Writers) ] ↑ ↓ [ Domain (DDD) ] [ Infrastructure (EF + Translators, Dapper, Views) ] + [ Domain Catalog (RDF export) + /domain UI ]

**Key principle:** *Domain knows nothing about EF/SQL. Infrastructure learns domain language.*

---

## Project Structure

| Layer                    | Projects                                                                              |
|--------------------------|---------------------------------------------------------------------------------------|
| **Core Domain**          | `VGR.Domain`, `VGR.Domain.Queries`, `VGR.Domain.Verifications`                        |
| **Application**          | `VGR.Application`                                                                     |
| **Semantic Core**        | `VGR.Semantics.Abstractions`, `VGR.Semantics.Linq`, `VGR.Semantics.Generator`        |
| **Infrastructure**       | `VGR.Infrastructure.EF`                                                               |
| **Delivery**             | `VGR.Web`, `VGR.Tests`                                                                |
| **Technical Domain**     | `VGR.Technical`, `VGR.Technical.Testing`                                              |
| **Quality & Guardrails** | `VGR.Analyzers`, `docs/*`                                                             |

---

## Consequences

### Positive

✅ **Epistemic clarity** – domain knowledge in one place, expressed in domain language  
✅ **Refactor-safety** – typed projections and semantic queries survive renames and restructures  
✅ **AI-native** – machine-readable semantics enable AI-assisted development (APPENDIX F)  
✅ **Performance maintained** – pushdown strategy guarantees database-level optimization (APPENDIX J)  
✅ **Code ergonomics** – developers navigate via IntelliSense in domain vocabulary  
✅ **Testability** – SqliteHarness enables fast, deterministic correlation tests  
✅ **Explainability** – semantic traces make decisions auditable  
✅ **Evolvability** – modular semantic components grow independently

### Negative

⚠️ **Learning curve** – developers must understand expression trees, semantic rewriting, and expansion rules  
⚠️ **Tooling requirements** – requires Roslyn analyzers, source generators, and semantic registry  
⚠️ **Convention strictness** – deviations from semantic patterns break translation guarantees  
⚠️ **Initial setup cost** – establishing semantic infrastructure requires upfront investment

### Mitigations

- **Comprehensive onboarding** (ONBOARDING.md)
- **AI guidance** (AI-GUIDANCE.md) for automated code generation
- **Analyzers** enforce architectural rules at compile-time
- **Correlation tests** validate semantic correctness automatically
- **Documentation-first** approach (this ADR index + appendices)

---

## Scope

### In Scope

This ADR applies to:
- All new development in VGR domain projects
- Application layer use cases
- Infrastructure query translation
- Domain modeling and semantic registration
- AI-assisted code generation

### Out of Scope

This ADR does **not** mandate:
- UI framework choices (React, Blazor, etc.)
- External service integration patterns (covered separately)
- Deployment infrastructure (Kubernetes, Docker, etc.)
- Specific CI/CD tooling

---

## Validation Criteria

This architectural decision is successful when:

1. ✅ New developers can navigate the codebase via domain vocabulary within 1 week
2. ✅ 95%+ of queries push down to SQL (measured via telemetry)
3. ✅ AI tools generate semantically correct code without manual correction
4. ✅ Domain changes propagate safely via refactoring tools
5. ✅ Correlation tests achieve 100% pass rate in CI/CD
6. ✅ p95 query latency remains < 50ms for indexed semantic queries

---

## Compliance

All code contributions must:
- [ ] Place new domain concepts in `VGR.Domain`
- [ ] Express queries via semantic patterns (APPENDIX H)
- [ ] Provide expansions for new domain methods requiring EF translation
- [ ] Include correlation tests validating SQL translation
- [ ] Use typed projections in `Domain.Queries` (no anonymous types)
- [ ] Document semantic attributes for registry discovery
- [ ] Follow naming conventions (semantic, not technical suffixes)

Violations are flagged by analyzers at compile-time.

---

## References

### Core Documents

- **ARCHITECTURE-CANON.md** – Complete architectural specification
- **ARCHITECTURE-NAME.md** – Naming rationale (E-Clean vs Semantic Architecture)
- **ARCHITECTURE-WHY.md** – Philosophical foundation
- **ONBOARDING.md** – Developer onboarding guide
- **AI-GUIDANCE.md** – AI assistant instructions

### Appendices

- **APPENDIX B** – Design Principles of Semantic Architecture
- **APPENDIX C** – Semantic Components
- **APPENDIX D** – Tooling Integration & The Roslyn Semantic Model
- **APPENDIX E** – Comparison with Clean Architecture & Domain-Driven Design
- **APPENDIX F** – AI-Assisted Development with Semantic Architecture
- **APPENDIX G** – Semantic Registry Specification
- **APPENDIX H** – Semantic Query Patterns
- **APPENDIX I** – Semantic Expansion Rules
- **APPENDIX J** – Performance & Query Optimization

### Related ADRs

- **ADR-001** – Index Policy (database performance)
- **ADR-002** – Semantic Names for Tests (code ergonomics)

---

## Revision History

| Version | Date       | Changes                           | Author              |
|---------|------------|-----------------------------------|---------------------|
| 1.0.0   | 2025-01-24 | Initial formal declaration        | VGR Architecture    |

---

## Conclusion

Epistemic Clean Architecture with Semantic Architecture implementation represents a fundamental shift:

**From:** Technical artifacts driving structure  
**To:** Domain language driving structure

**From:** Scattered epistemic sources  
**To:** Unified semantic truth

**From:** Manual translation overhead  
**To:** Systematic semantic execution

**From:** AI-opaque codebases  
**To:** AI-native semantic systems

This decision establishes the foundation for maintainable, evolvable, performant, and human-centric enterprise systems.

> *Language is the interface. Semantics execute.*

— **Epistemic Clean Architecture v1.0**