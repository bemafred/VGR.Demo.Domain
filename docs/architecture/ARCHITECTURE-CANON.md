# Epistemic Clean — Arkitekturkanon

**Tagline:** *Language is the interface. Semantics execute.*  
**Kort:** Clean/Hexagonal + domän→SQL semantisk översättar-adapter + domänkatalog (C#→RDF) + visuellt syskon till Swagger.

## Principer
1. **Pure Domain:** Aggregat & VO uttrycker semantik och invarians. Inga `IQueryable`/Expressions/EF i domänen.
2. **Semantisk Persistens:** EF-translators översätter domän-API (t.ex. `Tidsrymd.Överlappar`) till SQL. En sanningskälla för regler.
3. **Applikationsprosa:** Interaktorer uttrycker användningsfall med domänord, inte infrastruktur.
4. **Portar & Adaptrar:** Readers/Writers i applikations-abstraktioner; EF/Dapper/vyer bakom portar.
5. **Domänkatalog:** Begrepp/Relation/Regel som C#-attribut, export till Turtle. `/domain` visualiserar i runtime.
6. **Explainability-First:** Varje utbetalningsbeslut ska kunna förklaras; logga regler, indata, mellanresultat.
7. **Simplicity First:** Minsta nödvändiga lager. Introducera Specification/CQRS bara vid tydlig nytta.
8. **Verifiability:** Korrelationstester mellan domänpredikat (in-memory) och genererad SQL (`ToQueryString()`).
9. **Provider-portabilitet:** Translatorer har providerspecifika grenar (SqlServer/Npgsql/Sqlite) när det behövs.
10. **Observability-Ready:** OpenTelemetry (traces/metrics/logs) med etiketter: `ruleset`, `period`, `job_id`.

## Lager & beroenden
```
[ UI / API ]
   ↓
[ Application / Interactors ] —— calls ——> [ Ports (Readers/Writers) ]
   ↑                                              ↓
[ Domain (DDD) ]                          [ Infrastructure (EF + Translators, Dapper, Vyer) ]
                                     + [ Domain Catalog (RDF export) + /domain UI ]
```
*Domänen känner inte till EF/SQL. Infrastruktur lär sig domänens språk.*

**Konkreta projekt per lager/folder**

- **Core Domain** → `VGR.Domain`, `VGR.Domain.Queries`, `VGR.Domain.Tests`
- **Application (UseCases)** → `VGR.Application`
- **Semantic Core** → `VGR.Semantics.Abstractions`, `VGR.Semantics.Linq`, `VGR.Semantics.Generator`, `VGR.Semantics.Queries.Tests`
- **Infrastructure (Persistence & IO)** → `VGR.Infrastructure.EF`
- **Delivery (API & Hosting)** → `VGR.Web`, `VGR.Tests`
- **Technical Domain** → `VGR.Technical`, `VGR.Technical.Testing`
- **Quality & Guardrails** → `VGR.Analyzers`, `docs/*`

## Do / Don’t
**Do**
- Skriv queries som domänprosa: `v.Giltighet.ÄrTillsvidare`, `Överlappar`, `Innehåller`.
- Håll *all* filtreringssemantik i VO/aggregat; låt infra översätta.
- Använd små infra-extensions eller läs-readers i stället för generiska specs när möjligt.
- Märk endpoints med `[ApiConcept]` → `x-domain-concepts` i OpenAPI.
- Publicera `DomainCatalog.ttl` som build-artefakt.

**Don’t**
- Ingen `IQueryable`/`Expression` i Domain eller Application.
- Duplicera inte regler som “`Slut == null`” i app/infra — använd VO-API:t.
- Göm inte säkerhets/tenant-policy i interaktor; lägg i reader-port eller spec-policy.

## Minimikrav per modul (checklista)
- [ ] VO/aggregat med uttryckliga domänmetoder (verb) och invarians.
- [ ] Translator-stöd för relevanta VO-medlemmar/metoder (member/method translators).
- [ ] Reader/Writer-portar där infra behövs, små och semantiska.
- [ ] OpenAPI-annotering med `[ApiConcept]`.
- [ ] Domänkatalog-poster för nya begrepp/relationer/regler (+ URI).
- [ ] Korrelationstest: domänpredikat vs EF-SQL på representativ datamängd.
- [ ] Indexering/constraint (t.ex. partial unique för ”öppet”).
- [ ] OTel-etiketter i kritiska flöden.

## Referensmönster
- **Tidsrymd** (ägd typ): `ÄrTillsvidare`, `Överlappar`, `Innehåller`, `VararLängreÄn` → translators.
- **Vårdval**: `ÄrAktuellt` (deriverat), `Avsluta()`, `SkapaFör(Person)` → en källa, flera läsningar.
- **Ruleset** (immutable): semver + giltighet; signeras och länkas till körningar (audit).

## Kvalitetsmål
- Domänergonomi 5/5 · Testbarhet 4/5 · Prestanda 4/5 (→5 med vyer/Dapper) · Spårbarhet 5/5 · Evolvability 5/5.

— **Epistemic Clean** · *”Språket är gränssnittet. Semantiken exekverar.”*
