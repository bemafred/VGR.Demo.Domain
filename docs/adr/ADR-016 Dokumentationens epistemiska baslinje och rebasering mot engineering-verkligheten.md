# ADR-016: Dokumentationens epistemiska baslinje och rebasering mot engineering-verkligheten

## Status
Föreslagen

## Kontext

Referensarkitekturen har passerat emergence-fasen. Flera centrala arkitekturdrag är inte längre hypoteser utan verifierade genom faktisk kod, tester och runtime-yta.

Det gäller bland annat:

- `VGR.Application` använder direkt `ReadDbContext` och `WriteDbContext` i interactors.
- `VGR.Technical.Web` exponerar `/domain`, `/api` och `/data` som faktisk systemyta.
- Provider-verifieringar finns som egna projekt för PostgreSQL och SQL Server.
- Domänen är fri från EF och `IQueryable`, men använder semantisk metadata via `VGR.Semantics.Abstractions`.
- Guardrails i `VGR.Analyzers` omfattar idag `VGR001` och `VGR002`, men inte den i ADR-014 beskrivna `VGR003`.

Dokumentationen speglar däremot fortfarande i flera delar emergence-fasens tänkta arkitektur. Det leder till att vissa dokument beskriver intention, framtidsriktning och idealiserade lagerbilder som om de vore etablerad engineering-sanning.

### Problemet

När dokumentation från emergence, epistemics och engineering blandas utan explicit status uppstår epistemisk drift:

- Läsaren kan inte avgöra vad som är hypotes, vad som är beslutad riktning och vad som är implementerat.
- README, ADR:er, appendices och arkitekturkanon kan tala med olika sanningsnivåer.
- Dokumentation riskerar att bli aepistemisk genom att beskriva det möjliga som om det redan vore verkligt.

Samtidigt visar den aktuella kodbasen att repo:t redan har en faktisk arkitekturbaslinje som måste formuleras explicit — inte i termer av tänkt arkitektur, utan av den arkitektur som faktiskt blivit till.

## Beslut

### 1. All arkitekturdokumentation ska bära explicit epistemisk status

Varje normerande arkitekturdokument ska märkas med en av följande statusnivåer:

- **Emergence** — idé, hypotes, utforskning, möjlig riktning
- **Epistemically confirmed** — undersökt, klargjord och beslutad som sann beskrivning eller riktning
- **Engineered** — implementerad och verifierbar i faktisk kod/test/runtime
- **Deferred** — beslutad riktning som ännu inte engineering-realiserats
- **Superseded** — ersatt av nyare beslut eller verklig implementation

Detta gäller minst:

- `docs/architecture/*`
- `docs/appendix/*` när appendixen gör påståenden om faktisk arkitektur
- nya ADR:er där status behöver skilja mellan beslutad och implementerad verklighet

### 2. Kod är primär källa för engineering-påståenden

När ett dokument beskriver något som implementerat gäller följande ordning:

1. **Kod och tester** är primär källa till sanning om vad som är engineering-verklighet
2. **ADR:er** är beslutshistorik och normativ förklaring
3. **README** är aktuell sammanfattning av den etablerade referensarkitekturen
4. **ARCHITECTURE-CANON** är normativ kanon, men får inte beskriva oimplementerade delar som om de redan vore realiserade
5. **Appendices** får vara mer utforskande men måste då vara tydligt märkta som sådana

### 3. Referensarkitekturens aktuella baslinje definieras som semantiskt driven CQRS-light

Den aktuella referensarkitekturen ska beskrivas som:

> En semantiskt driven .NET-arkitektur där domänspråk uttrycks direkt i kod, översätts till SQL via en separat semantik-kärna, orkestreras i application-lagret via CQRS-light över `ReadDbContext` och `WriteDbContext`, verifieras genom korrelationstester och providerspecifika verifieringar, samt exponeras genom ett reflection-drivet system-UI.

### 4. Direkt DbContext-användning i Application är del av baslinjen

Det är etablerad arkitektur att `VGR.Application` får använda `ReadDbContext` och `WriteDbContext` direkt när detta:

- inte introducerar alternativ affärssanning
- inte flyttar regelns innebörd från domänen
- inte försvagar domänspråket
- inte ersätter domänbeteende med infrastrukturlogik

Detta ska beskrivas som ett medvetet val, inte som ett undantag från en tänkt portmodell.

### 5. "Ren domän" definieras som fri från persistence- och query-ceremoni, inte som semantikfri

Domänen ska fortsatt beskrivas som ren i betydelsen att den är fri från:

- EF Core-beroenden
- `IQueryable`
- `Expression`
- async- och cancellation-ceremoni
- teknisk persistenslogik

Men dokumentationen ska samtidigt uttryckligen säga att domänen får bära semantisk metadata genom attribut (`[SemanticQuery]`) när dessa stärker översättbarhet utan att föra in infrastrukturbeteende.

### 6. `VGR.Technical.Web` är förstaklassig del av referensarkitekturen

`/domain`, `/api` och `/data` ska dokumenteras som implementerade och centrala delar av referensarkitekturen — inte som appendix, demo-bonus eller framtidsriktning.

### 7. Providerspecifik verifiering är del av baslinjen

Referensarkitekturen är verifierad mot SQLite, PostgreSQL och SQL Server genom separata verifieringsprojekt. Detta är en del av arkitekturens engineering-anspråk.

### 8. Orealiserade delar ska flyttas från "är" till "riktning"

Funktioner, attribut, guardrails eller exporter som ännu inte kan verifieras i kodbasen ska inte beskrivas som etablerade delar av referensarkitekturen. Det gäller tills vidare:

- `VGR003` analyzer (ADR-014)
- Turtle/RDF-export (`DomainCatalog.ttl`)
- OpenTelemetry-instrumentering
- `[ApiConcept]` och OpenAPI-annotering
- Port-abstraktioner (IReader/IWriter)
- Ruleset-koncept

Dessa ska markeras som **Deferred** eller beskrivas under explicit riktning/framtida arbete.

### 9. Emergence-dokument ska bevaras, inte maskeras

Tidigare dokumentation från emergence-fasen ska inte skrivas om så att spåren av utforskningen försvinner. Den ska i stället:

- märkas med rätt epistemisk status
- behållas som designhistorik när den fortfarande tillför värde
- ersättas eller kompletteras när den inte längre beskriver aktuell sanning

## Konsekvenser

### Positiva

- Dokumentationen blir epistemiskt redlig
- Skillnaden mellan vision, beslut och implementation blir läsbar
- Referensarkitekturen kan presenteras utan glidning mellan idé och verklighet
- AI-assisterad läsning av repo:t blir mer tillförlitlig
- Arkitekturdokumenten kan användas som faktiska guardrails i stället för inspirationsmaterial
- Arkitekturens verkliga originalitet blir tydligare: semantik, översättning, verifiering och systemyta som sammanhängande helhet

### Negativa

- Fler dokument måste bära status och underhållas aktivt
- Vissa äldre texter kommer framstå som mindre "färdiga" när deras status görs explicit
- Övergångsperioden kräver genomgång av befintlig dokumentation
- Läsare som förväntar sig klassisk Clean Architecture-terminologi kan behöva ny orientering

### Invariant

Detta beslut förändrar inte arkitekturen i sig. Det deklarerar den arkitektur som redan etablerats genom epistemics och engineering, och inför sanningsdisciplin i dokumentationen.

## Implementationsstatus

- [ ] Statusfält införda i `docs/architecture/*`
- [ ] Statusfält införda där relevant i `docs/appendix/*`
- [ ] `ARCHITECTURE-CANON.md` harmoniserad med aktuell engineering-baslinje
- [ ] Oimplementerade riktningar omklassificerade till Deferred eller motsvarande
- [ ] Projektnamn och projektlistor korrigerade i alla dokument
- [ ] README, ADR:er och arkitekturkanon synkade mot samma sanningsnivå
- [ ] Tekniska verifieringsprojekt och system-UI lyfta till förstaklassig plats

## Relaterade dokument

- ADR-000 — Epistemic Clean & Semantic Architecture
- ADR-010 — Persistenslagret uttrycker relationella garantier för systeminvarianter
- ADR-011 — Compile-time verifiering av semantiska expansioner
- ADR-012 — Automatiskt domän-UI via reflection
- ADR-014 — Compile-time separation mellan delivery och infrastruktur
