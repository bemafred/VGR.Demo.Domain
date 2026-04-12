# ADR-017: Testförstärkning och verifieringsbaslinje

## Status
Föreslagen

## Kontext

Kodbasanalysen (v1.5.0) visar att arkitekturens kärna — domän, semantisk expression rewriting och EF-konfiguration — har stark testtäckning. Däremot saknas verifieringar i flera lager som bär produktionsansvar.

### Nuvarande testlandskap (104 tester)

| Projekt | Tester | Fokus |
|---------|--------|-------|
| `VGR.Domain.Verifications` | 47 | Aggregat, värdeobjekt, invarianter |
| `VGR.Semantics.Linq.Correlations` | 14 | Domänlogik ↔ SQL-ekvivalens |
| `VGR.Technical.Verifications` | 10 | `Utfall<T>`, tekniska byggblock |
| `VGR.Infrastructure.Diagnostics` | 6 | Schema, tracking, value conversions, RowVersion |
| `VGR.Application.Stories` | 5 | Enbart felvägar (saknas, ogiltigt) |
| `VGR.Web.Verifications` | 2 | Smoketester (happy path + överlapp) |
| `VGR.Semantics.Linq.Verifications` | 1 | Expression rewriting |
| `VGR.Infrastructure.PostgreSQL.Verifications` | 19 | Providerspecifikt |
| `VGR.Infrastructure.SqlServer.Verifications` | 10 (skippade) | Providerspecifikt |

### Identifierade luckor

1. **Application-lagret** — interactors har 5 tester, alla för felvägar. Inget test för lyckad person-skapning, lyckad vårdvalsregistrering, överlappskontroll via pushdown, eller automatisk avslutning av befintligt vårdval.
2. **Concurrency** — `RowVersion` konfigureras och sätts (testat), men det saknas verifiering av att en stale write faktiskt avvisas med `DbUpdateConcurrencyException`.
3. **HTTP-mappning** — `DomainMappingExtensions.Map() (VGR.Technical.Web.Mapping)` mappar 12+ feltyper till HTTP-statuskoder. Ingen av dessa mappningar verifieras.
4. **Provider-duplicering** — PostgreSQL- och SqlServer-korrelationstester duplicerar ~60% av SQLite-korrelationernas kod.
5. **Testdata** — varje test bygger upp entiteter manuellt. Inget återanvändbart builder-mönster.

## Beslut

### 1. Application-lagret ska ha verifieringar för lyckade och misslyckade flöden

`VGR.Application.Stories` utökas med tester som verifierar:

- **Lyckad personskapning:** Region + giltigt personnummer → `Utfall.Ok(PersonId)`, personen persisterad.
- **Lyckad vårdvalsskapning:** Person + giltig enhet + giltig period → `Utfall.Ok(VårdvalId)`, vårdvalet persisterat.
- **Överlappskontroll via pushdown:** Befintligt aktivt vårdval + nytt med överlappande period → `Utfall.Fail` (inte exception, utan pushdown-check i interactor).
- **Automatisk avslutning:** Nytt vårdval på samma enhet → befintligt vårdval avslutas med nytt startdatum.

Varje interactor ska ha minst ett test per utfallsgren.

### 2. Concurrency ska verifieras under samtidig mutation

`VGR.Infrastructure.Diagnostics` utökas med test som verifierar:

- Läs entitet via `ReadDbContext`, mutera och spara via `WriteDbContext`, ändra samma entitet via en **andra** `WriteDbContext`-instans → andra `SaveChangesAsync` ska kasta `DbUpdateConcurrencyException`.

Detta verifierar att `RowVersion`/`IsConcurrencyToken` faktiskt skyddar mot stale writes — inte bara att kolumnen finns.

### 3. HTTP-mappning ska verifieras per feltyp

`VGR.Web.Verifications` utökas med tester som verifierar att `DomainMappingExtensions.Map() (VGR.Technical.Web.Mapping)` producerar korrekt HTTP-statuskod för varje kanal:

| Ingång | Förväntad statuskod |
|--------|-------------------|
| `Utfall.Ok(value)` | 200 |
| `Utfall.Fail(error)` | 400 |
| `DomainInvariantViolationException` | 409 |
| `DomainAggregateNotFoundException` | 404 |
| `DomainValidationException` | 422 |
| `DomainConcurrencyConflictException` | 409 |
| `DomainArgumentFormatException` | 400 |
| `DomainUndefinedOperationException` | 422 |
| `DbUpdateConcurrencyException` | 409 |
| `OperationCanceledException` | 499 |

Testerna ska verifiera både statuskod och att `ProblemDetails.Extensions["code"]` innehåller korrekt maskinläsbar kod.

### 4. Korrelationstester ska generaliseras över providers

PostgreSQL- och SqlServer-korrelationstesterna delar ~60% av scenarierna med SQLite-korrelationerna. Dupliceringen ska reduceras genom att:

- Extrahera gemensamma testscenarier (MemberData) till en delad klass i `VGR.Technical.Testing` eller som `internal` i respektive testprojekt.
- Varje provider-projekt kör samma scenarier mot sin harness.
- Providerspecifika tester (t.ex. citering, filtered index-beteende) förblir i respektive projekt.

### 5. Testdata ska byggas med builder-mönster

`VGR.Technical.Testing` utökas med domänmedvetna builders:

```csharp
var (region, person) = await new TestScenario(harness)
    .MedRegion("VGR")
    .MedPerson("199001011234")
    .Bygg();
```

Builders ansvarar för:
- Korrekt skapandeordning (Region → Person → Vårdval)
- Deterministisk tid via `IClock`
- Sparning till `WriteDbContext`

Detta eliminerar repetitiv setup-kod (~10–15 rader per test) och gör nya tester billiga att skriva.

## Konsekvenser

### Positiva

- Alla lager med produktionsansvar får verifieringar
- Concurrency-beteende bevisas, inte bara konfigureras
- HTTP-mappningen blir en testad kontrakt, inte implicit kunskap
- Provider-testerna blir lättare att underhålla
- Nya tester blir billiga att skriva tack vare builders

### Negativa

- ~30–40 nya tester att underhålla
- Builder-mönstret kräver uppdatering vid domänförändringar
- Korrelationsgeneralisering kräver refaktorering av befintliga testprojekt

## Implementationsstatus

- [ ] Application.Stories: lyckade flöden för SkapaPersonInteractor
- [ ] Application.Stories: lyckade flöden för SkapaVårdvalInteractor
- [ ] Application.Stories: överlappskontroll och automatisk avslutning
- [ ] Infrastructure.Diagnostics: concurrency-test (stale write → exception)
- [ ] Web.Verifications: HTTP-mappning per feltyp (Utfall + alla DomainException-subtyper)
- [ ] Web.Verifications: ProblemDetails.Extensions["code"] verifieras
- [ ] Technical.Testing: TestScenario-builder
- [ ] Korrelationstester: gemensamma scenarier extraherade
- [ ] PostgreSQL/SqlServer-verifieringar: använder delade scenarier

## Relaterade dokument

- ADR-003 — Domänens semantiska felvokabulär
- ADR-005 — Felsemantik är verifierbar domänbeteende
- ADR-006 — Delivery översätter domänens felsemantik till transportsemantik
- ADR-009 — Produktionshärdning av delivery- och infrastrukturlager
- ADR-010 — Persistenslagret uttrycker relationella garantier för systeminvarianter
