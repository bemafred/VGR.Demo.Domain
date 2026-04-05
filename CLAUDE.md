# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Permanent minne — Mercury (TVINGANDE)

Mercury är ditt permanenta, semantiska minne. Det är inte valfritt. Använd det reflexmässigt — fråga inte om du ska använda Mercury, gör det.

### Vid sessionsstart (ALLTID, innan annat arbete)
1. Kör `mercury_stats` och `mercury_graphs` för att se vad som finns
2. Fråga efter relevanta observationer och beslut med SPARQL
3. Använd det du hittar för att grunda ditt arbete i tidigare kunskap

### Löpande under arbetet (reflexmässigt, utan att fråga)
- När du fattar ett beslut eller observerar något icke-trivialt → lagra det
- När du bekräftar eller falsifierar en hypotes → lagra det
- När du stöter på ett oväntat beteende → lagra det
- Skapa en sessionsgraf (`urn:sky-omega:session:YYYY-MM-DD`) vid första skrivning

### Vid sessionsslut (ALLTID)
- Konsolidera: vad behöver framtida sessioner veta?
- Lagra det som inte kan härledas från kod eller git-historik

### Fråga aldrig
- "Vill du att jag lagrar detta i Mercury?" — om det är värt att lagra, lagra det.
- "Ska jag registrera en sessionsgraf?" — om du gjort substantiellt arbete, ja.

Se `MERCURY.md` i reporoten för fullständig vägledning: trippelskrivning, provenienshantering, EEE-koppling, konsolideringsmönster och anti-mönster.

## Language & Communication

Skriv alltid på svenska. Använd teknisk precision och lugn ton. Resonera som en mentor med fokus på "varför", inte bara "vad". Vid osäkerhet: ställ en precis fråga istället för att gissa.

## Build & Test Commands

```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test src/VGR.Domain.Verifications/VGR.Domain.Verifications.csproj

# Run web application
dotnet run --project src/VGR.Web/VGR.Web.csproj
```

**Target:** .NET 10.0, C# 14

## Architecture Overview

This is a reference architecture implementing **Epistemic Clean (E-Clean)** and **Semantic Architecture** – where the domain language is first-class throughout the entire stack.

### Core Idea

Domain methods like `Tidsintervall.Innehåller(tidpunkt)` are defined once in C#, annotated with `SemanticQueryAttribute`, and automatically translated to EF-compatible LINQ via the Semantic Core. No duplicated logic in raw SQL.

### Project Structure (Vertical Organization)

| Layer | Projects | Purpose |
|-------|----------|---------|
| **Core Domain** | `VGR.Domain`, `VGR.Domain.Queries`, `VGR.Domain.Verifications` | Aggregates (Region, Person, Vårdval), value objects, invariants, domain-near queries |
| **Application** | `VGR.Application`, `VGR.Application.Stories` | Interactors (commands/queries) orchestrating domain + infrastructure |
| **Semantic Core** | `VGR.Semantics.Abstractions`, `VGR.Semantics.Linq`, `VGR.Semantics.Generator`, `VGR.Semantics.Linq.Verifications`, `VGR.Semantics.Linq.Correlations` | Query provider + expression rewriter translating domain methods to EF LINQ |
| **Infrastructure** | `VGR.Infrastructure.EF` | EF Core config, `ReadDbContext` (NoTracking), `WriteDbContext` |
| **Delivery** | `VGR.Web`, `VGR.Web.Verifications` | ASP.NET Core API, E2E tests |
| **Technical** | `VGR.Technical`, `VGR.Technical.Testing` | `Utfall<T>`, `IClock`, `SqliteHarness` |
| **Quality** | `VGR.Analyzers` | Roslyn rules enforcing domain integrity |

### Key Patterns

- **CQRS-light:** Separate `ReadDbContext` (NoTracking) for queries, `WriteDbContext` for commands
- **Clean Domain:** No EF, async, or infrastructure concerns in business logic
- **Error Handling:** `Throw` for invariants, `Utfall<T>` for non-exceptional failures
- **Semantic Test Naming:** `.Verifications` (verify behavior), `.Correlations` (correlate domain with SQL)

## Where to Place Code

**Preferred change direction:**
1. **Extend Domain** when a concept is missing
2. **Add semantic capabilities** in `VGR.Semantics.Linq` for query translation
3. **Add projections** in `Domain.Queries` for view shapes
4. Then wire up Infrastructure

**Never** encode new business "truth" only in queries, projections, or infrastructure.

## Domain Rules (Analyzer-Enforced)

- `VGR001`: No `public set` on domain properties
- `VGR002`: No public mutable collections (`ICollection`, `IList`, `List`)

Child collections: private `List<T>` + public `IReadOnlyList<T>`.

## Testing

All DB-accessing tests use `SqliteHarness` from `VGR.Technical.Testing`:

```csharp
await using var h = new SqliteHarness();
// h.Read - ReadDbContext, h.Write - WriteDbContext
```

- **Domain unit tests** (`VGR.Domain.Verifications`): No infrastructure, pure aggregates
- **Semantic correlations** (`VGR.Semantics.Linq.Correlations`): Verify domain methods match SQL translation
- **E2E tests** (`VGR.Web.Verifications`): Full stack with SQLite in-memory

## ADR-underhåll

ADR-dokument i `docs/adr/` har en `## Status`-sektion. Giltiga statusar: **Föreslagen**, **Accepterad**, **Genomförd**, **Ersatt**, **Avvisad**.

**Regel:** När du implementerar en ADR (eller verifierar att en ADR:s alla steg är klara), uppdatera dess status till `Genomförd`. Om en ADR har en implementationschecklista (`- [x]`/`- [ ]`), bocka av steg allteftersom de slutförs. Status och checklista ska alltid spegla verkligheten i koden.

## Code Principles

- **Simplicity First** – choose the most understandable solution
- **Domain is sovereign** – no EF/async/logging in domain layer
- **Swedish domain names**, English code identifiers
- **Minimal abstractions** – avoid over-engineering and premature generalization
