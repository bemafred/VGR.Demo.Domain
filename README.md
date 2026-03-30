# VGR Demo Domain – Epistemic Clean & Semantic Architecture (.NET / DDD / EF / CQRS-light)

Detta repo är en **referensarkitektur** för hur vi vill bygga domänstyrda system i .NET:

- **Epistemic Clean (E-Clean)** – principerna:  
  *språket är gränssnittet, semantiken exekverar*.
- **Semantic Architecture** – den konkreta implementationen i C#, EF Core och tooling.

Målet är att visa hur domänens språk kan vara första klassens medborgare genom hela stacken – från C#-modeller till SQL, tester och dokumentation – utan att drunkna i lager-ceremoni.

---

## 1. Vad är Epistemic Clean?

**Epistemic Clean** är ett sätt att tänka arkitektur där fokus ligger på:

- **Kunskap före teknik** – domänen uttrycks som begrepp, regler och relationer, inte som tabeller eller controllers och interactors.
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

> Vi vill kunna garantera domänens integritet samt skriva queries i domänspråk (t.ex. `tidsrymd.Innehåller(tidpunkt)`)
> och ändå få effektiv SQL, utan att duplicera regler i råa LINQ-uttryck eller komplicerande lagerkonstruktioner.

Det löses genom:

- En **ren domän** (`VGR.Domain`, `VGR.Domain.Queries`) utan EF/Expressions.
- En **Semantic Core**:
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
  - `VGR.Domain.Verifications` – tester av domän och domän-queries.

- **Application (UseCases)**
  - `VGR.Application` – interaktorer (kommandon/queries) som orkestrerar domän + infra.
  - `VGR.Application.Stories` – applikationsnära stories/scenarion.

- **Semantic Core**
  - `VGR.Semantics.Abstractions` – attribut och kontrakt för semantiska queries.
  - `VGR.Semantics.Linq` – query-provider + expression-rewriter (`WithSemantics`, semantik-register).
  - `VGR.Semantics.Generator` – source generator som bygger registret.
  - `VGR.Semantics.Linq.Verifications` – tester av semantisk översättning.
  - `VGR.Semantics.Linq.Correlations` – korrelationstester för domänlogik mot SQL-översättning.

- **Infrastructure (Persistence & IO)**
  - `VGR.Infrastructure.EF` – EF Core-konfiguration, `ReadDbContext`, `WriteDbContext` (CQRS-light, pushdown).
  - `VGR.Infrastructure.Diagnostics` – infrastrukturrelaterad verifiering/diagnostik.

- **Delivery (API & Hosting)**
  - `VGR.Web` – ASP.NET Core-API, controllers, hosting.
  - `VGR.Web.Verifications` – E2E-/integrationstester mot interaktorer/webb (SQLite in-memory).

- **Technical Domain**
  - `VGR.Technical` – tekniska byggblock (`Utfall`, `IClock`, m.m.).
  - `VGR.Technical.Testing` – stödfunktioner för tester (`SqliteHarness`, m.m.).
  - `VGR.Technical.Verifications` – verifiering av tekniska byggblock.

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
  - Semantic Core översätter dessa till EF-kompatibla uttryck.
  - Vi undviker duplicerad logik som `Start <= t && (Slut == null || t < Slut)` spridd i LINQ.
  - Semantisk persistens är avancerat men behöver normalt inte hanteras av utvecklare.

- **Felhantering med `Throw` och `Utfall`**  
  - `Throw` för invariants och fel som ska bryta exekveringen.
  - `Utfall<T>` för icke-exceptionella misslyckanden och tydliga resultat i interaktorer.

- **Guardrails via analyzers**  
  Roslyn-regler säkerställer att domänen inte smittas av infrastrukturnära kod (t.ex. publika set, muterbara samlingar).

- **Kodergonomi**  
  Kod ska kännas som domänprosa, inte som ramverkskonfiguration. Se `docs/KODERGONOMI.md`.

---

| Solution folder                       | Projekt                      | Syfte                                                                                             |
|---------------------------------------|------------------------------|---------------------------------------------------------------------------------------------------|
| **Core Domain**                       | `VGR.Domain`                 | Verksamhetsdomän: aggregat, värdeobjekt, invariants, `Throw`.                                     |
|                                       | `VGR.Domain.Queries`         | Domännära queries/predikat (utan EF-beroende).                                                    |
|                                       | `VGR.Domain.Verifications`   | Enhetstester av domänen och domän-queries (utan infrastruktur).                                   |
| **Application (UseCases)**            | `VGR.Application`            | Interaktorer (kommandon och queries) som orkestrerar domän + infrastruktur.                       |
|                                       | `VGR.Application.Stories`    | Applikationsnära stories/scenarion och verifiering.                                               |
| **Semantic Core**                 | `VGR.Semantics.Abstractions` | Attribut och kontrakt för semantiska queries (`SemanticQueryAttribute`, `ExpansionForAttribute`). |
|                                       | `VGR.Semantics.Linq`         | Query-provider + expression-rewriter (`WithSemantics()`, `SemanticRegistry`) för domän→EF-LINQ.   |
|                                       | `VGR.Semantics.Generator`    | Source generator som bygger upp semantik-registret vid compile-time.                              |
|                                       | `VGR.Semantics.Linq.Verifications` | Tester av semantisk översättning och query-beteende.                                        |
|                                       | `VGR.Semantics.Linq.Correlations` | Korrelationstester för domänlogik mot SQL-översättning.                                     |
| **Infrastructure (Persistence & IO)** | `VGR.Infrastructure.EF`      | Entity Framework-konfiguration och `DbContext` (Read/Write, pushdown-strategi).                   |
|                                       | `VGR.Infrastructure.Diagnostics` | Infrastrukturrelaterad verifiering/diagnostik.                                               |
| **Delivery (API & Hosting)**          | `VGR.Web`                    | ASP.NET Core-API, controllers, hosting.                                                           |
|                                       | `VGR.Web.Verifications`      | End-to-end/integrationstester mot interaktorer och webb (SQLite in-memory).                       |
| **Technical Domain**                  | `VGR.Technical`              | Teknisk domän: `Utfall`, `Map`, `IClock`, intern infrastruktur för interaktorer & controllers.    |
|                                       | `VGR.Technical.Testing`      | Stödfunktioner för testning.                                                                      |
|                                       | `VGR.Technical.Verifications` | Verifiering av tekniska byggblock.                                                               |
| **Quality & Guardrails**              | `VGR.Analyzers`              | Roslyn-analyzers som upprätthåller domänregler.                                                   |
| **Architecture & Docs**               | `docs/*`                     | Arkitektur- och policy-dokumentation (`ANALYS`, `PLACERING`, `POLICY`, `KODERGONOMI`, m.fl.).     |

## Principer

- **Domänen är suverän** – inga beroenden till EF, applikation eller infrastruktur.
- **Semantic Core** är den enda platsen där domänens språk översätts till EF-vänliga uttryck:
  - domänmetoder/predikat annoteras via `SemanticQueryAttribute`/`ExpansionForAttribute`,
  - `VGR.Semantics.Linq` och `VGR.Semantics.Generator` bygger upp ett centralt semantik-register.
- **Felhantering sker med `Throw` eller `Utfall`.**  
  - `Throw` används för invariants och fel som *ska* bryta exekveringen – både i domän och applikationslager.  
  - `Utfall` kan användas när det finns skäl att undvika undantag, t.ex. av prestandaskäl eller för att uttrycka icke-exceptionella misslyckanden i interaktorer.
- **Application** implementerar interaktorer som anropar domänen, utnyttjar Semantic Core för queries och returnerar `Utfall`/kastar vid behov.
- **Infrastructure.EF** ansvarar för mappning, persistens och pushdown-konfiguration (Read/Write DbContexts).
- **Delivery** (Web + E2E) exponerar användningsfall via HTTP och testar hela kedjan från API till DB.
- **Analyzers** säkerställer att inga regler bryts i domänen (t.ex. public set, publika `List<>`).
- **CQRS-light** används för att separera kommandon (skrivoperationer) från queries (läsoperationer), utan att införa onödig ceremoni.

Vertikal placering av projekt (inklusive testprojekt) följer dessa principer:  
*varje “världsdels-mapp” (Core Domain, Semantic Core, Delivery, …) innehåller både kod och dess verifiering.*

- **Core Domain**
  - `VGR.Domain` – rika aggregat (Region, Person, Vårdval), VO:s, invariants, Domain Events.
  - `VGR.Domain.Queries` – domännära queries (inte bundna till EF).

- **Application (UseCases)**
  - `VGR.Application` – interaktorer (kommandon/queries) som orkestrerar domänen och infrastrukturen.

- **Semantic Core**
  - `VGR.Semantics.Abstractions` – attribut och kontrakt för semantiska queries (t.ex. `SemanticQueryAttribute`, `ExpansionForAttribute`).
  - `VGR.Semantics.Linq` – query-provider + expression-rewriter (`WithSemantics`, `SemanticRegistry`) som översätter domänmetoder till EF-vänlig LINQ.
  - `VGR.Semantics.Generator` – source generator som bygger upp semantik-registret.
  - `VGR.Semantics.Linq.Verifications` – tester av den semantiska plattformen.
  - `VGR.Semantics.Linq.Correlations` – tester av expansioner för korrekt översättning till SQL.

- **Infrastructure (Persistence & IO)**
  - `VGR.Infrastructure.EF` – EF Core-konfigurationer, `ReadDbContext`, `WriteDbContext`, pushdown-strategi.

- **Delivery (API & Hosting)**
  - `VGR.Web` – ASP.NET Core API, controllers, HTTP-mappning.
  - `VGR.Web.Verifications` – E2E-/integrations-tester (xUnit + SQLite in-memory) mot interaktorer/webb.

- **Technical Domain**
  - `VGR.Technical` – tekniska byggblock (t.ex. `Utfall`, `IClock`).
  - `VGR.Technical.Testing` – stödfunktioner för testning (t.ex `SqliteHarness`).

- **Quality & Guardrails**
  - `VGR.Analyzers` – Roslyn-regler för domänen (`VGR001`, `VGR002`).
  - `docs/*` – arkitektur- och policy-dokumentation.

## Struktur
```
src/
  VGR.Domain/
    SharedKernel/ (VO + exceptions + Throw)
    Region.cs, Person.cs, Vårdval.cs
  VGR.Domain.Verifications/
  VGR.Domain.Queries/
  VGR.Application/
  VGR.Application.Stories/
  VGR.Semantics.Abstractions/
  VGR.Semantics.Generator/
  VGR.Semantics.Linq/
  VGR.Semantics.Linq.Verifications/
  VGR.Semantics.Linq.Correlations/
  VGR.Technical/
    Utfall.cs, IClock.cs
  VGR.Technical.Testing/
    SqliteHarness.cs
  VGR.Technical.Verifications/
  VGR.Infrastructure.EF/
    Configs/ (PersonConfig, VårdvalConfig, RegionConfig)
    Expansions/ (VårdvalExpansions, TidsrymdExpansions)
    ReadDbContext.cs, WriteDbContext.cs
  VGR.Infrastructure.Diagnostics/
  VGR.Web/
  VGR.Web.Verifications/
```

## Principer
- Domän är **ren & synkron**: inga EF/async/ct; mutation via beteenden.
- Barnmängder: **privat List<T>** + **publik IReadOnlyList<T>**.
- CQRS-light: **ReadDbContext** (NoTracking), **WriteDbContext** (tracking).
- Pushdown först: läsningar som projektion/Any; skrivningar laddar minsta nödvändiga.
- Semantisk persistens: domänmetoder (t.ex. `Tidsrymd.Överlappar`, `Innehåller`) översätts centralt via Semantic Core till SQL-vänliga uttryck.

## Bygg och kör

Kräver .NET SDK som stöder `net10.0`.

### Kör webben (demo)
```bash
dotnet run --project src/VGR.Web/VGR.Web.csproj
```

`VGR.Web` använder en InMemory-databas för demo-hosting i nuvarande form.

### Bygg lösningen
```bash
dotnet build VGR.Demo.Domain.sln
```

### Kör tester
```bash
dotnet test VGR.Demo.Domain.sln
```

## Tester (xUnit + SQLite in-memory)

Projekt **VGR.Web.Verifications** använder SQLite in-memory och kör end-to-end mot interaktorerna.
Projekt **VGR.Semantics.Linq.Correlations** använder SQLite in-memory och kör expansionstester mot SQL.

- **Domän-enhetstester** (`VGR.Domain.Verifications`): testar aggregat och värdeobjekt utan EF/infrastruktur.
  ```bash
  dotnet test src/VGR.Domain.Verifications/VGR.Domain.Verifications.csproj
  ```

- **Semantiska verifieringar** (`VGR.Semantics.Linq.Verifications`, `VGR.Semantics.Linq.Correlations`): testar expression-rewriting och SQL-korrelationer.
  ```bash
  dotnet test src/VGR.Semantics.Linq.Verifications/VGR.Semantics.Linq.Verifications.csproj
  dotnet test src/VGR.Semantics.Linq.Correlations/VGR.Semantics.Linq.Correlations.csproj
  ```

- **Webb-/integrationsverifieringar** (`VGR.Web.Verifications`): kör applikationsflöden mot SQLite in-memory.
  ```bash
  dotnet test src/VGR.Web.Verifications/VGR.Web.Verifications.csproj
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
    - `docs/ARCHITECTURE-WHY.md`

- **Projektstruktur och ansvar** – `docs/PLACERING.md` (inkl. översikt över Appendix A–I)
- **Övergripande analys** – `docs/ANALYS.md`

- **Policyer (Throw/Outcome, CQRS-light)** – `docs/POLICY.md`

- **Kodergonomi** – `docs/KODERGONOMI.md`

Den här README:n är “portalen” – för detaljerad arkitektur och motivation, gå vidare in i `docs/`.
