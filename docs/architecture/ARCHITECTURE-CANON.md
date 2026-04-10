# Epistemic Clean — Arkitekturkanon

> **Epistemisk status: Engineered**
> Dokumentet beskriver den aktuella engineering-baslinjen. Delar som ännu inte realiserats
> i kod är samlade under "Framtida riktning" i slutet.

**Tagline:** *Language is the interface. Semantics execute.*  
**Kort:** Semantiskt driven CQRS-light + domän→SQL översättning via expression rewriting + reflection-drivet system-UI (`/domain`, `/data`, `/api`).

## Principer
1. **Pure Domain:** Aggregat & VO uttrycker semantik och invarians. Inga `IQueryable`/Expressions/EF i domänen. Semantisk metadata via `[SemanticQuery]` är tillåtet.
2. **Semantisk Persistens:** EF-translators översätter domän-API (t.ex. `Tidsrymd.Överlappar`) till SQL. En sanningskälla för regler.
3. **Applikationsprosa:** Interaktorer uttrycker användningsfall med domänord. `ReadDbContext`/`WriteDbContext` injiceras direkt som medvetet arkitekturval.
4. **CQRS-light:** Separat `ReadDbContext` (NoTracking) för queries och `WriteDbContext` för kommandon — utan ceremoni.
5. **Domänkatalog:** Begrepp och relationer som C#-attribut. `/domain` visualiserar domänmodellen i runtime.
6. **Simplicity First:** Minsta nödvändiga lager. Introducera abstraktioner bara vid tydlig nytta.
7. **Verifiability:** Korrelationstester mellan domänpredikat (in-memory) och genererad SQL.
8. **Provider-portabilitet:** Translatorer har providerspecifika grenar (SqlServer/Npgsql/Sqlite) verifierade genom separata testprojekt.

## Lager & beroenden
```
[ UI / API ]  +  [ System-UI: /domain, /data, /api ]
   ↓
[ Application / Interactors ] —— använder ——> [ ReadDbContext / WriteDbContext ]
   ↑                                                    ↓
[ Domain (DDD) ]                              [ Infrastructure (EF + Expansions) ]
```
*Domänen känner inte till EF/SQL. Infrastruktur lär sig domänens språk via semantiska expansioner.*

**Konkreta projekt per lager/folder**

- **Core Domain** → `VGR.Domain`, `VGR.Domain.Queries`, `VGR.Domain.Verifications`
- **Application (UseCases)** → `VGR.Application`, `VGR.Application.Stories`
- **Semantic Core** → `VGR.Semantics.Abstractions`, `VGR.Semantics.Linq`, `VGR.Semantics.Generator`, `VGR.Semantics.Linq.Verifications`, `VGR.Semantics.Linq.Correlations`
- **Infrastructure (Persistence & IO)** → `VGR.Infrastructure.EF`, `VGR.Infrastructure.Diagnostics`, `VGR.Infrastructure.PostgreSQL.Verifications`, `VGR.Infrastructure.SqlServer.Verifications`
- **Delivery (API & Hosting)** → `VGR.Web`, `VGR.Web.Verifications`
- **Technical Domain** → `VGR.Technical`, `VGR.Technical.Testing`, `VGR.Technical.Web`, `VGR.Technical.Verifications`
- **Quality & Guardrails** → `VGR.Analyzers`, `docs/*`

## Do / Don't
**Do**
- Skriv queries som domänprosa: `v.Giltighet.ÄrTillsvidare`, `Överlappar`, `Innehåller`.
- Håll *all* filtreringssemantik i VO/aggregat; låt infra översätta.
- Använd `WithSemantics()` i interactors för att aktivera expression rewriting.
- Håll domänen fri från EF, `IQueryable`, `Expression`, async och persistenslogik.

**Don't**
- Duplicera inte regler som "`Slut == null`" i app/infra — använd VO-API:t.
- Ingen `IQueryable`/`Expression` i Domain.
- Inför inte abstraktioner (portar, repositories) utan bevisad nytta.

## Minimikrav per modul (checklista)
- [ ] VO/aggregat med uttryckliga domänmetoder (verb) och invarians.
- [ ] Translator-stöd för relevanta VO-medlemmar/metoder (expansion med `[ExpansionFor]`).
- [ ] Korrelationstest: domänpredikat vs EF-SQL på representativ datamängd.
- [ ] Indexering/constraint (t.ex. partial unique för "öppet").

## Referensmönster
- **Tidsrymd** (ägd typ): `ÄrTillsvidare`, `Överlappar`, `Innehåller`, `VararLängreÄn` → expansions.
- **Vårdval**: `ÄrAktivt` (deriverat), `Avsluta()`, `Person.SkapaVårdval()` → en källa, flera läsningar.

## Kvalitetsmål
- Domänergonomi 5/5 · Testbarhet 4/5 · Prestanda 4/5 · Spårbarhet 5/5 · Evolvability 5/5.

## Framtida riktning (Deferred)

Följande delar är beslutade riktningar eller möjliga utvidgningar som ännu inte är engineering-realiserade:

- **Port-abstraktioner:** Readers/Writers-portar i applikations-abstraktioner. Idag används `DbContext` direkt.
- **Turtle/RDF-export:** `DomainCatalog.ttl` som build-artefakt. Domänkatalogen visualiseras idag via `/domain` i HTML.
- **OpenTelemetry:** Traces/metrics/logs med etiketter (`ruleset`, `period`, `job_id`).
- **`[ApiConcept]`:** OpenAPI-annotering med `x-domain-concepts`.
- **VGR003 analyzer:** Compile-time separation mellan delivery och infrastruktur (ADR-014).
- **Ruleset:** Immutable regelset med semver + giltighet, signerade och länkade till körningar.
- **Explainability-First:** Loggning av regler, indata och mellanresultat för varje beslut.

— **Epistemic Clean** · *"Språket är gränssnittet. Semantiken exekverar."*
