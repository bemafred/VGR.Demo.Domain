# VGR Demo Domain – Epistemic Clean & Semantic Architecture (.NET / DDD / EF / CQRS-light)

**Version 1.0.0** — .NET 10.0 / C# 14 / EF Core / PostgreSQL & SQL Server / 123 tester / 16 ADR:er

*Avancerat och enkelt är bättre än komplicerat och naivt.*

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
- **Förklarbarhet** – beslut och regler ska kunna förklaras i efterhand ("varför blev utfallet så här?").
- **Strukturerad komplexitet** – vi tar inte bort komplexitet, vi placerar den där den hör hemma (domän, semantik, infrastruktur).

Principerna finns destillerade i:

- `docs/architecture/ARCHITECTURE-CANON.md`
- `docs/architecture/ARCHITECTURE-NAME.md`
- `docs/architecture/ARCHITECTURE-WHY.md`

---

## 2. Vad är Semantic Architecture?

**Semantic Architecture** är *implementationen* av E-Clean i den här lösningen.

Nyckelidé:

> Vi vill kunna garantera domänens integritet samt skriva queries i domänspråk (t.ex. `tidsrymd.Innehåller(tidpunkt)`)
> och ändå få effektiv SQL, utan att duplicera regler i råa LINQ-uttryck eller komplicerande lagerkonstruktioner.
> Detta ersätter explicita rituella lager i legacy arkitekturer som Ports & Adapters med mera.
> Det **tillåter** direkt åtkomst till EF-kontexten, EF-kontexten i sig isolerar lösningen från databasen.
> Att byta ORM är därför *inte nödvändigt* - EF Core är en del av .NET och stöds av Microsoft.

Det löses genom:

- En **ren domän** (`VGR.Domain`, `VGR.Domain.Queries`) utan EF/Expressions.
- En **Semantic Core**:
  - attribut (`SemanticQueryAttribute`, `ExpansionForAttribute`) för att märka domänmetoder,
  - ett semantik-register (`SemanticRegistry`) och query-provider (`WithSemantics`) som skriver om domänmetoder till EF-vänlig LINQ,
  - en generator som verifierar att alla semantiska metoder har expansioner vid compile-time.
- En **Infrastructure.EF** som fokuserar på pushdown, indexering och ren mapping.
- En **Delivery-lina** (Web + E2E-tests) som visar hur allt binds samman.

---

## 3. Projektstruktur

Solution-folderstrukturen speglar ansvarsområden. Uppdelningen är **vertikal**: varje "världsdel" innehåller både kod och tester.

| Solution folder | Projekt | Syfte |
|---|---|---|
| **Core Domain** | `VGR.Domain` | Aggregat (Region, Person, Vårdval), värdeobjekt, invariants, `Throw`. |
| | `VGR.Domain.Queries` | Domännära queries/predikat (utan EF-beroende). |
| | `VGR.Domain.Verifications` | Enhetstester av domänen och domän-queries. |
| **Application (UseCases)** | `VGR.Application` | Interaktorer (kommandon/queries) som orkestrerar domän + infrastruktur. |
| | `VGR.Application.Stories` | Applikationsnära stories/scenarion och verifiering. |
| **Technical Domain** | `VGR.Technical` | Tekniska byggblock (`Utfall`, `IClock`). |
| | `VGR.Technical.Testing` | Stödfunktioner för testning (`SqliteHarness`). |
| | `VGR.Technical.Web` | System-UI: `app.MapDomainEndpoints()`, reflection-driven `/domain`- och `/api`-vyer. |
| | `VGR.Technical.Verifications` | Verifiering av tekniska byggblock. |
| **Semantic Core** | `VGR.Semantics.Abstractions` | Attribut och kontrakt (`SemanticQueryAttribute`, `ExpansionForAttribute`). |
| | `VGR.Semantics.Linq` | Query-provider + expression-rewriter (`WithSemantics()`, `SemanticRegistry`). |
| | `VGR.Semantics.Generator` | Source generator — compile-time verifiering av semantiska expansioner. |
| | `VGR.Semantics.Linq.Verifications` | Tester av semantisk översättning och query-beteende. |
| | `VGR.Semantics.Linq.Correlations` | Korrelationstester: domänlogik mot SQL-översättning. |
| **Infrastructure** | `VGR.Infrastructure.EF` | EF Core-konfiguration, `ReadDbContext`, `WriteDbContext` (CQRS-light). |
| | `VGR.Infrastructure.Diagnostics` | Infrastrukturrelaterad verifiering/diagnostik. |
| | `VGR.Infrastructure.PostgreSQL.Verifications` | Integrationstester mot riktig PostgreSQL. |
| | `VGR.Infrastructure.SqlServer.Verifications` | Integrationstester mot riktig SQL Server. |
| **Delivery (API & Hosting)** | `VGR.Web` | ASP.NET Core-API, controllers, HTTP-mappning. |
| | `VGR.Web.Verifications` | E2E-/integrationstester (SQLite in-memory). |
| **Quality & Guardrails** | `VGR.Analyzers` | Roslyn-regler för domänen (`VGR001`, `VGR002`). |
| **Tools** | `tools/debug-semantics` | Debugverktyg för semantiska expansioner. |
| **Architecture & Docs** | `docs/*` | ADR:er, arkitektur, guides, appendices. |

```
src/
  VGR.Domain/
    SharedKernel/ (VO + exceptions + Throw)
    Region.cs, Person.cs, Vårdval.cs
  VGR.Domain.Queries/
  VGR.Domain.Verifications/
  VGR.Application/
  VGR.Application.Stories/
  VGR.Technical/
    Utfall.cs, IClock.cs
  VGR.Technical.Testing/
    SqliteHarness.cs, PostgresHarness.cs, SqlServerHarness.cs
    KräverPostgresFactAttribute.cs, KräverSqlServerFactAttribute.cs
  VGR.Technical.Web/
    DomainEndpoints.cs, DataEndpoints.cs, DomainPage.cs, ApiPage.cs
  VGR.Technical.Verifications/
  VGR.Semantics.Abstractions/
  VGR.Semantics.Generator/
  VGR.Semantics.Linq/
  VGR.Semantics.Linq.Verifications/
  VGR.Semantics.Linq.Correlations/
  VGR.Infrastructure.EF/
    Configs/ (PersonConfig, VårdvalConfig, RegionConfig)
    Expansions/ (TidsrymdExpansions, VårdvalExpansions)
    ReadDbContext.cs, WriteDbContext.cs
  VGR.Infrastructure.Diagnostics/
  VGR.Infrastructure.PostgreSQL.Verifications/
  VGR.Infrastructure.SqlServer.Verifications/
  VGR.Web/
  VGR.Web.Verifications/
tools/
  debug-semantics/
```

---

## 4. Centrala principer

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

- **Guardrails via analyzers**
  Roslyn-regler säkerställer att domänen inte smittas av infrastrukturnära kod (t.ex. publika set, muterbara samlingar).
  Barnmängder: privat `List<T>` + publik `IReadOnlyList<T>`.

- **Kodergonomi**
  Kod ska kännas som domänprosa, inte som ramverkskonfiguration. Se `docs/guides/KODERGONOMI.md`.

---

## 5. Felhantering – från domän till HTTP

Arkitekturen har en genomtänkt pipeline för fel och undantag som sträcker sig från domänens invarianter hela vägen till HTTP-svar. Tre komponenter samverkar:

### Throw – domänspråklig undantagsfabrik

`Throw` är en statisk fasadklass med nestade domänklasser som kastar semantiskt namngivna undantag:

```csharp
Throw.Vårdval.ÖverlappEjTillåtet(enhetHsaId, giltighet);
Throw.Person.Dubblett(personnummer);
Throw.Region.Saknas(regionId);
```

Varje undantag har en **stabil maskinläsbar kod** (t.ex. `"Vårdval.ÖverlappEjTillåtet"`) och ärver från `DomainException`. Hierarkin klassificerar felet semantiskt:

| Undantagstyp | Betydelse |
|---|---|
| `DomainInvariantViolationException` | En affärsregel har brutits |
| `DomainInvalidStateTransitionException` | Otillåten tillståndsövergång |
| `DomainValidationException` | Ogiltigt värde (format/semantik) |
| `DomainAggregateNotFoundException` | Aggregat/objekt saknas |
| `DomainConcurrencyConflictException` | Optimistisk samtidighetskonflikt |
| `DomainIdempotencyViolationException` | Kommandot har redan utförts |
| `DomainArgumentFormatException` | Felaktigt format på indata |
| `DomainUndefinedOperationException` | Odefinierad domänoperation |

Att hierarkin finns är inte ceremoni – den möjliggör den centraliserade HTTP-mappningen längre ut i stacken.

### Utfall\<T\> – resultattyp utan undantag

`Utfall<T>` kompletterar `Throw` för situationer där misslyckande är förväntat och inte bör bryta exekveringen:

```csharp
// Dubblettcheck i interaktor – förväntat utfall, inget undantag
if (dubblett)
    return Utfall<PersonId>.Fail("Personnummer redan registrerat");

return Utfall<PersonId>.Ok(person.Id);
```

**Tumregel:** `Throw` för invarianter och strukturella fel. `Utfall.Fail()` för affärsmässigt rimliga nekanden.

### DomainMappingExtensions.Map() – centraliserad HTTP-mappning

I delivery-lagret binder `Map()`-extensionmetoden ihop allt. Varje controller-action delegerar till `Map()`, som:
1. Exekverar interaktorn i en try/catch
2. Översätter `Utfall` och domänundantag till RFC 9457-kompatibla `ProblemDetails`

```csharp
// Controller – tunn, ingen egen felhantering
[HttpPost]
public async Task<IActionResult> Skapa(Guid personId, [FromBody] SkapaVårdvalDto body, CancellationToken ct)
    => await this.Map(ct => interactor.ProcessAsync(command, ct), ct);
```

Mappningen:

| Källa | HTTP-status | Betydelse |
|---|---|---|
| `Utfall.Ok(value)` | 200 OK | Lyckad operation |
| `Utfall.Fail(error)` | 400 Bad Request | Förväntat affärsfel |
| `DomainArgumentFormatException` | 400 Bad Request | Formatfel i indata |
| `DomainValidationException` | 422 Unprocessable Entity | Ogiltigt värde |
| `DomainAggregateNotFoundException` | 404 Not Found | Aggregat saknas |
| `DomainInvariantViolationException` | 409 Conflict | Affärsregel bruten |
| `DomainInvalidStateTransitionException` | 409 Conflict | Otillåten tillståndsövergång |
| `DomainConcurrencyConflictException` | 409 Conflict | Samtidighetskonflikt |
| `DomainIdempotencyViolationException` | 409 Conflict | Redan utfört |
| `DomainUndefinedOperationException` | 422 Unprocessable Entity | Odefinierad operation |
| `DbUpdateConcurrencyException` | 409 Conflict | Infrastruktur-concurrency |
| `DbUpdateException` | 422 Unprocessable Entity | Databaskonstraint bruten |
| `OperationCanceledException` | 499 Client Closed Request | Klienten avbröt |
| Övriga undantag | 500 Internal Server Error | Oväntat fel |

Genom att all översättning sker på **ett ställe** blir controllers tunna, felhanteringen konsekvent, och nya undantagstyper kan mappas centralt utan att ändra i varje endpoint.

---

## 6. Bygg och kör

Kräver .NET SDK som stöder `net10.0`. Webapplikationen använder SQL Server på Windows och PostgreSQL på macOS (villkorat i `Program.cs`).

### Kör webben

```bash
dotnet run --project src/VGR.Web/VGR.Web.csproj
```

Webben exponerar fyra system-UI-sidor via `app.MapDomainEndpoints()`:
- `/` — indexsida
- `/domain` — reflection-driven domänstrukturvy
- `/api` — reflection-driven endpoint-vy
- `/data` — reflection-driven datavy (CRUD mot domäntyper)

### Bygg lösningen

```bash
dotnet build VGR.Demo.Domain.sln
```

### Kör tester

```bash
# Alla tester — DB-specifika tester skippas automatiskt om servern inte är nåbar
dotnet test VGR.Demo.Domain.sln
```

Testprojekt och vad de verifierar:

| Projekt | Verifierar | Databas |
|---|---|---|
| `VGR.Domain.Verifications` | Aggregat, värdeobjekt, invarianter | Ingen |
| `VGR.Semantics.Linq.Verifications` | Expression-rewriting, query-beteende | SQLite |
| `VGR.Semantics.Linq.Correlations` | Domänlogik ↔ SQL-översättning | SQLite |
| `VGR.Application.Stories` | Interaktorer, felvägar | SQLite |
| `VGR.Web.Verifications` | E2E applikationsflöden | SQLite |
| `VGR.Technical.Verifications` | `Utfall`, tekniska byggblock | Ingen |
| `VGR.Infrastructure.Diagnostics` | Schema, index, NoTracking, RowVersion | SQLite |
| `VGR.Infrastructure.PostgreSQL.Verifications` | Infrastruktur + korrelationer mot PostgreSQL | PostgreSQL |
| `VGR.Infrastructure.SqlServer.Verifications` | Infrastruktur + korrelationer mot SQL Server | SQL Server |

---

## 7. Analyzers (Domain guardrails)

Projekt **VGR.Analyzers** innehåller Roslyn-regler som appliceras på `VGR.Domain`:
- `VGR001` – förhindrar `public set` på domän-egenskaper
- `VGR002` – förhindrar publika muterbara samlingar (ICollection/IList/List)

Se `docs/guides/ANALYZER_REGLER.md`. Severity konfigureras i `.editorconfig` (default = error).

---

## 8. Dokumentation

| Kategori | Sökväg | Innehåll |
|---|---|---|
| **ADR:er** | `docs/adr/ADR-000..015` | 16 arkitekturbeslut (E-Clean, testnamn, felhantering, härdning, expansioner, domän-UI, DB-providrar) |
| **Arkitektur** | `docs/architecture/` | CANON, NAME, WHY — destillerade principer |
| **Guides** | `docs/guides/` | ANALYS, POLICY, KODERGONOMI, PLACERING, ONBOARDING, QUICKSTART, ANALYZER_REGLER |
| **Appendices** | `docs/appendix/` | A–J: plattform, design, komponenter, tooling, jämförelser, AI, registry, patterns, regler, prestanda |
