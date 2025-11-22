# VGR Arkitektur — PLACERING

Detta dokument beskriver hur de olika projekten i lösningen är organiserade och vilken roll de spelar i helheten.

## Struktur (per solution folder)

| Solution folder                       | Projekt                          | Syfte                                                                                             |
|---------------------------------------|----------------------------------|---------------------------------------------------------------------------------------------------|
| **Core Domain**                       | `VGR.Domain`                     | Verksamhetsdomän: aggregat, värdeobjekt, invariants, `Throw`.                                     |
|                                       | `VGR.Domain.Queries`             | Domännära queries/predikat (utan EF-beroende).                                                    |
|                                       | `VGR.Domain.Tests`               | Enhetstester av domänen och domän-queries (utan infrastruktur).                                   |
| **Application (UseCases)**            | `VGR.Application`                | Interaktorer (kommandon och queries) som orkestrerar domän + infrastruktur.                       |
| **Semantic Core**                 | `VGR.Semantics.Abstractions`     | Attribut och kontrakt för semantiska queries (`SemanticQueryAttribute`, `ExpansionForAttribute`). |
|                                       | `VGR.Semantics.Linq`             | Query-provider + expression-rewriter (`WithSemantics`, `SemanticRegistry`) för domän→EF-LINQ.     |
|                                       | `VGR.Semantics.Generator`        | Source generator som bygger upp semantik-registret vid compile-time.                              |
|                                       | `VGR.Semantics.Linq.Tests`       | Tester av semantisk översättning och query-beteende.                                              |
|                                       | `VGR.Semantics.Linq.CorrelationTests` | Korrelationstester av domänmetoder mot SQL via SQLite in-memory.                              |
| **Infrastructure (Persistence & IO)** | `VGR.Infrastructure.EF`          | Entity Framework-konfiguration och `DbContext` (Read/Write, pushdown-strategi).                   |
| **Delivery (API & Hosting)**          | `VGR.Web`                        | ASP.NET Core-API, controllers, hosting.                                                           |
|                                       | `VGR.Tests`                      | End-to-end/integrationstester mot interaktorer och webb (SQLite in-memory).                       |
| **Technical Domain**                  | `VGR.Technical`                  | Teknisk domän: `Utfall`, `Map`, `IClock`, intern infrastruktur för interaktorer.                  |
|                                       | `VGR.Technical.Testing`          | **Testinfrastruktur**: `SqliteHarness` för unified in-memory EF-testning (Read/Write DbContexts).  |
| **Quality & Guardrails**              | `VGR.Analyzers`                  | Roslyn-analyzers som upprätthåller domänregler.                                                   |
| **Architecture & Docs**               | `docs/*`                         | Arkitektur- och policy-dokumentation (`ANALYS`, `PLACERING`, `POLICY`, `KODERGONOMI`, m.fl.).     |

## Principer

- **Domänen är suverän** – inga beroenden till EF, applikation eller infrastruktur.
- **Semantic Core** är den enda platsen där domänens språk översätts till EF-vänliga uttryck:
    - domänmetoder/predikat annoteras via `SemanticQueryAttribute`/`ExpansionForAttribute`,
    - `VGR.Semantics.Linq` och `VGR.Semantics.Generator` bygger upp ett centralt semantik-register.
- **Felhantering sker med `Throw` eller `Utfall`.**
    - `Throw` används för invariants och fel som *ska* bryta exekveringen – både i domän och applikationslager.
    - `Utfall` kan användas när det finns skäl att undvika undantag, t.ex. av prestandaskäl eller för att uttrycka icke-exceptionella misslyckanden i interaktorer.
- **Technical Domain** (`VGR.Technical`, `VGR.Technical.Testing`) innehåller tekniska begrepp som är:
    - Ortogonala till affärsdomänen (kan användas från flera håll)
    - Designbeslut (t.ex. `Utfall<T>` för resultat-hantering)
    - Infrastruktur-stöd (t.ex. `SqliteHarness` för testning)
    - **Princip**: Minimalt men nödvändigt. Ingen "utility-bibliotek"-fälla.
- **Application** implementerar interaktorer som anropar domänen, utnyttjar Semantic Core för queries och returnerar `Utfall` eller kastar med `Throw` vid behov.
- **Infrastructure.EF** ansvarar för mappning, persistens och pushdown-konfiguration (Read/Write DbContexts).
- **Delivery** (Web + E2E) exponerar användningsfall via HTTP och testar hela kedjan från API till DB.
- **Analyzers** säkerställer att inga regler bryts i domänen (t.ex. public set, publika `List<>`).
- **CQRS-light** används för att separera kommandon (skrivoperationer) från queries (läsoperationer), utan att införa onödig ceremoni.

Vertikal placering av projekt (inklusive testprojekt) följer dessa principer:  
*varje “världsdels-mapp” (Core Domain, Semantic Core, Delivery, …) innehåller både kod och dess verifiering.*