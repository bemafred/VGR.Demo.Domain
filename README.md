# VGR Arkitektur — PLACERING

Detta dokument beskriver hur de olika projekten i lösningen är organiserade och vilken roll de spelar i helheten.

## Struktur (per solution folder)

| Solution folder                  | Projekt                              | Syfte |
|----------------------------------|--------------------------------------|-------|
| **Core Domain**                  | `VGR.Domain`                         | Verksamhetsdomän: aggregat, värdeobjekt, invariants, `Throw`. |
|                                  | `VGR.Domain.Queries`                | Domännära queries/predikat (utan EF-beroende). |
|                                  | `VGR.Domain.Tests`                  | Enhetstester av domänen och domän-queries (utan infrastruktur). |
| **Application (UseCases)**       | `VGR.Application`                   | Interaktorer (kommandon och queries) som orkestrerar domän + infrastruktur. |
| **Semantic Platform**            | `VGR.Semantics.Abstractions`        | Attribut och kontrakt för semantiska queries (`SemanticQueryAttribute`, `ExpansionForAttribute`). |
|                                  | `VGR.Semantics.Queries`             | Query-provider + expression-rewriter (`WithSemantics`, `SemanticRegistry`) för domän→EF-LINQ. |
|                                  | `VGR.Semantics.Generator`           | Source generator som bygger upp semantik-registret vid compile-time. |
|                                  | `VGR.Semantics.Queries.Tests`       | Tester av semantisk översättning och query-beteende. |
| **Infrastructure (Persistence & IO)** | `VGR.Infrastructure.EF`         | Entity Framework-konfiguration och `DbContext` (Read/Write, pushdown-strategi). |
| **Delivery (API & Hosting)**     | `VGR.Web`                           | ASP.NET Core-API, controllers, hosting. |
|                                  | `VGR.Tests`                         | End-to-end/integrationstester mot interaktorer och webb (SQLite in-memory). |
| **Technical Kernel**             | `VGR.Technical`                     | Teknisk domän: `Outcome`, `Map`, `IClock`, intern infrastruktur för interaktorer. |
| **Quality & Guardrails**         | `VGR.Analyzers`                     | Roslyn-analyzers som upprätthåller domänregler. |
| **Architecture & Docs**          | `docs/*`                            | Arkitektur- och policy-dokumentation (`ANALYS`, `PLACERING`, `POLICY`, `KODERGONOMI`, m.fl.). |

## Principer

- **Domänen är suverän** – inga beroenden till EF, applikation eller infrastruktur.
- **Semantic Platform** är den enda platsen där domänens språk översätts till EF-vänliga uttryck:
  - domänmetoder/predikat annoteras via `SemanticQueryAttribute`/`ExpansionForAttribute`,
  - `VGR.Semantics.Queries` och `VGR.Semantics.Generator` bygger upp ett centralt semantik-register.
- **Felhantering sker med `Throw` eller `Outcome`.**  
  - `Throw` används för invariants och fel som *ska* bryta exekveringen – både i domän och applikationslager.  
  - `Outcome` kan användas när det finns skäl att undvika undantag, t.ex. av prestandaskäl eller för att uttrycka icke-exceptionella misslyckanden i interaktorer.
- **Application** implementerar interaktorer som anropar domänen, utnyttjar Semantic Platform för queries och returnerar `Outcome`/kastar vid behov.
- **Infrastructure.EF** ansvarar för mappning, persistens och pushdown-konfiguration (Read/Write DbContexts).
- **Delivery** (Web + E2E) exponerar användningsfall via HTTP och testar hela kedjan från API till DB.
- **Analyzers** säkerställer att inga regler bryts i domänen (t.ex. public set, publika `List<>`).
- **CQRS-light** används för att separera kommandon (skrivoperationer) från queries (läsoperationer), utan att införa onödig ceremoni.

Vertikal placering av projekt (inklusive testprojekt) följer dessa principer:  
*varje “världsdels-mapp” (Core Domain, Semantic Platform, Delivery, …) innehåller både kod och dess verifiering.*

- **Core Domain**
  - `VGR.Domain` – rika aggregat (Region, Person, Vårdval), VO:s, invariants, Domain Events.
  - `VGR.Domain.Queries` – domännära queries (inte bundna till EF).

- **Application (UseCases)**
  - `VGR.Application` – interaktorer (kommandon/queries) som orkestrerar domänen och infrastrukturen.

- **Semantic Platform**
  - `VGR.Semantics.Abstractions` – attribut och kontrakt för semantiska queries (t.ex. `SemanticQueryAttribute`, `ExpansionForAttribute`).
  - `VGR.Semantics.Queries` – query-provider + expression-rewriter (`WithSemantics`, `SemanticRegistry`) som översätter domänmetoder till EF-vänlig LINQ.
  - `VGR.Semantics.Generator` – source generator som bygger upp semantik-registret.
  - `VGR.Semantics.Queries.Tests` – tester av den semantiska plattformen.

- **Infrastructure (Persistence & IO)**
  - `VGR.Infrastructure.EF` – EF Core-konfigurationer, `ReadDbContext`, `WriteDbContext`, pushdown-strategi.

- **Delivery (API & Hosting)**
  - `VGR.Web` – ASP.NET Core API, controllers, HTTP-mappning.
  - `VGR.Tests` – E2E-/integrations-tester (xUnit + SQLite in-memory) mot interaktorer/webb.

- **Technical Kernel**
  - `VGR.Technical` – tekniska byggblock (t.ex. `Utfall`, `Dq`, `IClock`).

- **Quality & Guardrails**
  - `VGR.Analyzers` – Roslyn-regler för domänen (`VGR001`, `VGR002`).
  - `docs/*` – arkitektur- och policy-dokumentation.

## Struktur (förenklad)

## Struktur
```
src/
  VGR.Domain/
    SharedKernel/ (VO + exceptions + Throw)
    Region.cs, Person.cs, Vardval.cs
  VGR.Semantics.Abstractions/
  VGR.Semantics.Generator/
  VGR.Sementics.Queries/
  VGR.Sementics.Queries.Tests/
  VGR.Technical/
    Utfall.cs, Dq.cs
  VGR.Infrastructure.EF/
    Configs/ (PersonConfig, VardvalConfig, RegionConfig)
    ReadDbContext.cs, WriteDbContext.cs
```

## Principer
- Domän är **ren & synkron**: inga EF/async/ct; mutation via beteenden.
- Barnmängder: **privat List<T>** + **publik IReadOnlyList<T>**.
- CQRS-light: **ReadDbContext** (NoTracking), **WriteDbContext** (tracking).
- Pushdown först: läsningar som projektion/Any; skrivningar laddar minsta nödvändiga.
- Semantisk persistens: domänmetoder (t.ex. `Tidsrymd.Överlappar`, `Innehåller`) översätts centralt via Semantic Platform till SQL-vänliga uttryck.

## Bygg
Kräver .NET SDK som stöder `net10.0`
```csharp
services.AddDbContext<ReadDbContext>(o => o.UseSqlServer(cs)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
services.AddDbContext<WriteDbContext>(o => o.UseSqlServer(cs));
```
## Application + Web

Nu finns även:
- `VGR.Application` med `SkapaPersonInteractor` (pushdown + fabrik på Region).
- `VGR.Web` med en `PersonsController`, en enkel `Map`-klass för Outcome/Exceptions→HTTP, och `Program.cs` (InMemory DB för demo).

### Kör webben (demo)
```bash
dotnet run --project src/VGR.Web/VGR.Web.csproj
# POST http://localhost:5000/api/regioner/{regionId}/personer
```


### Nya exempel (Interactor + Controller)
- `VGR.Application/Vardval/SkapaVardvalInteractor.cs` – pushdown-överlappskontroll, filtrerad explicit load, `Person.SkapaVardval(...)`, save.
- `VGR.Web/Controllers/VardvalController.cs` – `POST /api/personer/{personId}/vardval` med DTO.


## Tester (xUnit + SQLite in-memory)

Projekt **VGR.Tests** använder SQLite in-memory (relations-likt) och kör end-to-end mot interaktorerna.

Kör tester:
```bash
dotnet test src/VGR.Tests/VGR.Tests.csproj -c Release
```

Tester som ingår:
- `CreatePerson_Then_CreateVardval_Succeeds` – E2E: Region → SkapaPerson → SkapaVårdval → verifiera persistens
- `CreateVardval_Overlapping_ReturnsFail` – pushdown-kollen stoppar överlapp


## Tester

- **Domän-enhetstester** (`VGR.Domain.Tests`): testar aggregat/VO utan EF/infrastruktur.

  ```bash
  dotnet test src/VGR.Domain.Tests/VGR.Domain.Tests.csproj -c Release
  ```

- **E2E-/integrationstester** (`VGR.Tests`): kör mot interaktorer/webb med SQLite in-memory.

  ```bash
  dotnet test src/VGR.Tests/VGR.Tests.csproj -c Release
  ```

## Analyzers (Domain guardrails)

Projekt **VGR.Analyzers** innehåller Roslyn-regler som appliceras på `VGR.Domain`:
- `VGR001` – förhindrar `public set` på domän-egenskaper
- `VGR002` – förhindrar publika muterbara samlingar (ICollection/IList/List)

Se `ANALYZER_REGLER.md`. Severity konfigureras i `.editorconfig` (default = error).
