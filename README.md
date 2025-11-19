# VGR Demo Domain – Epistemic Clean & Semantic Architecture (.NET / EF / CQRS-light)

Detta repo är en **referensarkitektur** för hur vi vill bygga domänstyrda system i .NET:

- **Epistemic Clean (E-Clean)** – principerna:  
  *språket är gränssnittet, semantiken exekverar*.
- **Semantic Architecture** – den konkreta implementationen i C#, EF Core och tooling.

Målet är att visa hur domänens språk kan vara första klass genom hela stacken – från C#-modeller till SQL, tester och dokumentation – utan att drunkna i lager-ceremoni.

---

## 1. Vad är Epistemic Clean?

**Epistemic Clean** är ett sätt att tänka arkitektur där fokus ligger på:

- **Kunskap före teknik** – domänen uttrycks som begrepp, regler och relationer, inte som tabeller eller controllers.
- **Språk som gränssnitt** – kod ska kunna läsas som domänprosa: `person.SkapaVårdval()`, `tidsrymd.Överlappar(annan)`.
- **Förklarbarhet** – beslut och regler ska kunna förklaras i efterhand (”varför blev utfallet så här?”).
- **Strukturerad komplexitet** – vi tar inte bort komplexitet, vi placerar den där den hör hemma (domän, semantik, infrastruktur).

Principerna finns destillerade i:

- `docs/ARCHITECTURE-CANON.md`
- `docs/ARCHITECTURE-NAME.md`
- `docs/ARCHITECTURE-WHY.md`

---

## 2. Vad är Semantic Architecture?

**Semantic Architecture** är *implementationen* av E-Clean i den här lösningen.

Nyckelidé:

> Vi vill kunna skriva queries i domänspråk (t.ex. `tidsrymd.Innehåller(tidpunkt)`)  
> och ändå få effektiv SQL, utan att duplicera regler i råa LINQ-uttryck.

Det löses genom:

- En **ren domän** (`VGR.Domain`, `VGR.Domain.Queries`) utan EF/Expressions.
- En **Semantic Platform**:
  - attribut (`SemanticQueryAttribute`, `ExpansionForAttribute`) för att märka domänmetoder,
  - ett semantik-register (`SemanticRegistry`) och query-provider (`WithSemantics`) som skriver om domänmetoder till EF-vänlig LINQ,
  - en generator som bygger registret vid compile-time.
- En **Infrastructure.EF** som fokuserar på pushdown, indexering och ren mapping.
- En **Delivery-lina** (Web + E2E-tests) som visar hur allt binds samman.

---

## 3. Projekt och solution-folders (mental karta)

Solution-folderstrukturen speglar ansvarsområden:

- **Core Domain**
  - `VGR.Domain` – aggregat, VO, invariants, `Throw`.
  - `VGR.Domain.Queries` – domännära queries/predikat.
  - `VGR.Domain.Tests` – tester av domän och domän-queries.

- **Application (UseCases)**
  - `VGR.Application` – interaktorer (kommandon/queries) som orkestrerar domän + infra.

- **Semantic Platform**
  - `VGR.Semantics.Abstractions` – attribut och kontrakt för semantiska queries.
  - `VGR.Semantics.Queries` – query-provider + expression-rewriter (`WithSemantics`, semantik-register).
  - `VGR.Semantics.Generator` – source generator som bygger registret.
  - `VGR.Semantics.Queries.Tests` – tester av semantisk översättning.

- **Infrastructure (Persistence & IO)**
  - `VGR.Infrastructure.EF` – EF Core-konfiguration, `ReadDbContext`, `WriteDbContext` (CQRS-light, pushdown).

- **Delivery (API & Hosting)**
  - `VGR.Web` – ASP.NET Core-API, controllers, hosting.
  - `VGR.Tests` – E2E-/integrationstester mot interaktorer/webb (SQLite in-memory).

- **Technical Kernel**
  - `VGR.Technical` – tekniska byggblock (`Outcome`, `Dq`, `IClock`, m.m.).

- **Quality & Guardrails**
  - `VGR.Analyzers` – Roslyn-regler för domänen (t.ex. inga `public set`, inga publika `List<>`).
  - `docs/*` – arkitektur, policy, kodergonomi.

Den här uppdelningen är **vertikal**: varje “världsdel” innehåller både kod och tester, snarare än en horisontell “alla tester här”-mapp.

---

## 4. Centrala principer i den här implementationen

Några av de viktigaste principerna som demonstreras:

- **Ren och synkron domän**  
  Domänlagret har inga beroenden på EF, async, cancellation tokens eller `IQueryable`. Mutation sker via beteenden, inte via `set`.

- **CQRS-light med pushdown**  
  - `ReadDbContext` (NoTracking) för queries och projektioner.
  - `WriteDbContext` för aggregerad persistens vid kommandon.
  - Läsningar pushas ner till SQL; skrivningar laddar minsta nödvändiga data för att skydda invariants.

- **Semantisk persistens**  
  - Domänmetoder som `Tidsrymd.Innehåller`, `Överlappar` uttrycks en gång i domän/semantik.
  - Semantic Platform översätter dessa till EF-kompatibla uttryck.
  - Vi undviker duplicerad logik som `Start <= t && (Slut == null || t < Slut)` spridd i LINQ.

- **Felhantering med `Throw` och `Outcome`**  
  - `Throw` för invariants och fel som ska bryta exekveringen.
  - `Outcome<T>` för icke-exceptionella misslyckanden och tydliga resultat i interaktorer.

- **Guardrails via analyzers**  
  Roslyn-regler säkerställer att domänen inte smittas av infrastrukturnära kod (t.ex. publika set, muterbara samlingar).

- **Kodergonomi**  
  Kod ska kännas som domänprosa, inte som ramverkskonfiguration. Se `docs/KODERGONOMI.md`.

---

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

## 6. Var ska jag läsa mer?

- **Övergripande analys** – `docs/ANALYS.md`  
  Hur arkitekturen bedöms (styrkor/svagheter, mätpunkter).

- **Principer och namn** –
    - `docs/ARCHITECTURE-CANON.md`
    - `docs/ARCHITECTURE-NAME.md`
    - `docs/EpistemicClean-VarförDetKännsBekvämt.md`

- **Projektplacering och ansvar** – `docs/PLACERING.md`

- **Policyer (Throw/Outcome, CQRS-light)** – `docs/POLICY.md`

- **Kodergonomi** – `docs/KODERGONOMI.md`

Den här README:n är “portalen” – för detaljerad arkitektur och motivation, gå vidare in i `docs/`.
