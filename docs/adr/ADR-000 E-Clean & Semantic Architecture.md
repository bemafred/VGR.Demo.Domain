# ADR-000: Epistemic Clean & Semantic Architecture

***Formell deklaration av arkitekturmönster och implementationsprinciper***

---

## Metadata

| Fält                    | Värde                                     |
|-------------------------|--------------------------------------------|
| **Status**              | Accepterad                                 |
| **Version**             | 1.1.0                                      |
| **Datum**               | 2025-01-24                                 |
| **Beslutsfattare**      | VGR Architecture Team                      |
| **Ersätter**            | Ingen (grundläggande)                      |
| **Relaterade ADR:er**   | ADR-001 (Indexpolicy), ADR-002 (Semantiska namn), ADR-003 (Felvokabulär), ADR-004 (Semantisk precision), ADR-005 (Verifierbar felsemantik), ADR-006 (Transportöversättning), ADR-007 (Dubbel felkanal), ADR-008 (Värdeobjektskontrakt), ADR-009 (Produktionshärdning), ADR-010 (Relationella garantier) |

---

## Kontext

Traditionella .NET-arkitekturer brottas med flera grundläggande problem:

1. **Semantisk drift** – domänkunskap sprids över lager (controllers, services, repositories, databas)
2. **Epistemisk fragmentering** – ”sanningen” finns i flera former (C#-modeller, SQL-scheman, API-kontrakt, dokumentation)
3. **Översättningskostnad** – konstant mentalt kontextbyte mellan domänspråk och tekniska artefakter
4. **AI-inkompatibilitet** – kodbaser saknar maskinläsbar semantisk struktur
5. **Skör refaktorering** – domänförändringar sprider sig oförutsägbart genom infrastrukturen
6. **Prestanda vs tydlighet** – optimerade frågor offrar ofta domänuttryckskraft

Existerande angreppssätt (Clean Architecture, DDD, CQRS) adresserar vissa av dessa, men:

- De löser inte **det semantiska översättningsproblemet** (domän → databas)
- De tillhandahåller inte **maskinläsbara domänkataloger**
- De optimerar inte för **kodergonomi** som förstaklassmedborgare
- De integrerar inte **AI-assisterad utveckling** som en grundprincip

Vi behövde en arkitektur som:

- Håller domänkunskap **ren och central**
- Gör semantik **exekverbar** genom systematisk översättning
- Ser **språk som primärt gränssnitt**
- Möjliggör resonemang för **både människor och AI**
- Bibehåller **produktionsmässig prestanda** trots abstraktioner

---

## Beslut

Vi antar **Epistemic Clean Architecture (E-Clean)** som vårt arkitekturmönster och **Semantic Architecture** som dess konkreta implementation.

### Vad är E-Clean?

**Epistemic Clean Architecture (E-Clean)** är ett arkitekturmönster som:

- Bygger på en **ren, rik domän** som uttrycker kunskap och domäninvarianter
- Upprätthåller **strikt separering** mellan domän (epistemisk) och infrastruktur (teknisk)
- Uppnår **kodergonomi** genom språkliga uttryck snarare än tekniska artefakter
- Använder **systematisk semantisk översättning** från domänspråk till exekverbara frågor

**Tagline:** *Language is the interface. Semantics execute.*

### Vad är Semantic Architecture?

**Semantic Architecture** är den konkreta implementationen av E-Clean-principerna genom:

1. **Semantic Registry** – maskinläsbar katalog över domänbegrepp, queries, projektioner och relationer
2. **Semantic Queries** – uttrycksbaserade domänåtkomstmönster (APPENDIX H)
3. **Semantic Expansions** – deklarativa berikningsregler över komponentgränser (APPENDIX I)
4. **Expression Tree Rewriting** – översättning av domänmetoder till EF-kompatibel SQL
5. **Projektionstyrd exekvering** – typade read-modeller som bevarar semantisk innebörd
6. **Domain Catalog** – RDF/Turtle-export av domänstruktur för visualisering och AI-resonemang

**Central skillnad:**
> **E-Clean definierar *hur* vi bygger. Semantic Architecture definierar *vad* vi bygger.**

Precis som MVC är ett mönster, och ASP.NET MVC är implementationen.

---

## Arkitekturprinciper

### 1. Ren domän
Aggregat och värdeobjekt uttrycker semantik och invarianter.  
**Ingen** `IQueryable`, `Expression` eller EF i domänlagret.

### 2. Semantisk persistens
EF-översättare omvandlar domän-API:er (t.ex. `Tidsrymd.Överlappar`) till SQL.  
En källa till sanning för verksamhetsregler.

### 3. Applikationen som verkställighetsstrategiskt lager
Interaktorer uttrycker use cases med domänspråk, inte infrastrukturtermer.
Applikationslagret ansvarar också för att välja **verkställighetsstrategi** — vilken kunskap som hämtas, i vilket exekveringssätt regeln tillämpas, och hur lite data som behöver laddas för att bevara korrekthet. Tillåtna strategier: semantisk pushdown, selektiv hydrering, precheck i fråga, relationell constraint, concurrency-kontroll, full hydrering (se ADR-010).

### 4. Epistemisk separation utan ceremoniella portar
E-Clean definierar separation genom **kunskapsansvar**, inte genom obligatoriska tekniska mellanlager.

Det betyder:
- domänen äger sanningen
- semantiklagret äger översättbarheten
- applikationslagret äger verkställighetsstrategin
- infrastrukturen äger persistens- och providerbeteende

Om dessa ansvar hålls rena är arkitekturen korrekt även utan repository-/portlager. Ett direkt beroende från Application till Infrastructure.EF är först ett problem om det introducerar alternativ affärssanning, flyttar regelns innebörd från domänen, eller gör use caset beroende av tekniska artefakter som försvagar domänspråket.

### 5. Domänkatalog
Begrepp/Relationer/Regler som C#-attribut, exporterbara till Turtle.  
`/domain` endpoint visualiserar vid runtime.

### 6. Förklarbarhet först
Varje beslut ska vara spårbart: logga regler, input, mellanresultat.

### 7. Simplicity First
Minimalt antal lager.  
Introducera Specification/CQRS endast vid tydlig nytta.

### 8. Verifierbarhet
Korrelations­tester mellan domänpredikat (in-memory) och genererad SQL (`ToQueryString()`).

### 9. Leverantörsoberoende
Översättare har providerspecifika grenar (SqlServer/Npgsql/Sqlite) vid behov.

### 10. Observability-ready
OpenTelemetry (traces/metrics/logs) med semantiska taggar: `ruleset`, `period`, `job_id`.

---

## Tolkningsram

När en läsare eller ett AI-verktyg utvärderar ett use case ska analysen följa denna ordning:

1. Identifiera om problemet gäller **sanning**, **översättning**, **verkställighetsstrategi** eller **persistensgaranti**.
2. Avgör om det finns en faktisk alternativ sanning, eller bara ett brott mot ett klassiskt mönster.
3. Föreslå inte nya lager innan det visats att problemet inte kan lösas inom den befintliga epistemiska modellen.
4. Bedöm alltid om en föreslagen abstraktion tillför semantisk disciplin eller bara teknisk ceremoni.

**Normerande frågor** i denna arkitektur:

- Var uttrycks sanningen?
- Var uppstår semantisk drift?
- Har en alternativ sanning introducerats?
- Är verkställighetsstrategin explicit?
- Är den valda strategin proportionerlig mot regelns natur?

**Inte normerande i sig:** Finns ett repository-interface? Finns en fysisk port? Känner Application till EF? Är varje regel verkställd via full aggregatladdning?

Följande default-reflexer ska betraktas som otillförlitliga tills motsatsen visats:

- "lägg till ett repository"
- "lägg till interfaces för att rena lagren"
- "ladda hela aggregatet"
- "ta bort direkt infra-beroende bara för att ett lagerdiagram annars ser renare ut"

En förändring är bara motiverad om den minskar epistemisk drift, ökar förklarbarhet eller ger en verklig korrekthetsvinst.

---

## Arkitekturlager

[ UI / API ]↓
[ Application / Interactors ] —— anropar ——>
[ Domain (DDD) ] + [ Infrastructure (EF + Semantic Translators) ]
+ [ Domain Catalog (RDF-export) + /domain UI ]

**Nyckelprincip:** *Domänen vet inget om EF/SQL. Infrastruktur lär sig domänspråket.*
**Separationsprincip:** Epistemisk, inte ceremoniell.

---

## Projektstruktur

| Lager                   | Projekt                                                                              |
|-------------------------|---------------------------------------------------------------------------------------|
| **Core Domain**         | `VGR.Domain`, `VGR.Domain.Queries`, `VGR.Domain.Verifications`                        |
| **Application**         | `VGR.Application`, `VGR.Application.Stories`                                           |
| **Semantic Core**       | `VGR.Semantics.Abstractions`, `VGR.Semantics.Linq`, `VGR.Semantics.Generator`, `VGR.Semantics.Linq.Verifications`, `VGR.Semantics.Linq.Correlations` |
| **Infrastructure**      | `VGR.Infrastructure.EF`, `VGR.Infrastructure.Diagnostics`                              |
| **Delivery**            | `VGR.Web`, `VGR.Web.Verifications`                                                    |
| **Technical Domain**    | `VGR.Technical`, `VGR.Technical.Testing`, `VGR.Technical.Verifications`               |
| **Quality & Guardrails**| `VGR.Analyzers`, `docs/*`                                                             |

---

## Konsekvenser

### Positiva

✅ **Epistemisk klarhet** – domänkunskap samlad på ett ställe, uttryckt i domänspråk  
✅ **Refaktorsäkerhet** – typade projektioner och semantiska queries överlever renames och omstruktureringar  
✅ **AI-native** – maskinläsbar semantik möjliggör AI-assisterad utveckling (APPENDIX F)  
✅ **Bibehållen prestanda** – pushdown-strategi säkerställer databasoptimering (APPENDIX J)  
✅ **Kodergonomi** – utvecklare navigerar via IntelliSense i domänens vokabulär  
✅ **Testbarhet** – SqliteHarness möjliggör snabba, deterministiska korrelationstester  
✅ **Förklarbarhet** – semantiska traces gör beslut auditbara  
✅ **Evolvbarhet** – modulära semantiska komponenter växer oberoende

### Negativa

⚠️ **Inlärningskurva** – utvecklare måste förstå expression trees, semantisk rewriting och expansionsregler  
⚠️ **Verktygskrav** – kräver Roslyn-analyzers, source generators och semantisk registry  
⚠️ **Strikta konventioner** – avvikelser från semantiska mönster bryter översättningsgarantier  
⚠️ **Initial investeringskostnad** – semantisk infrastruktur kräver uppsättning i början

### Mitigeringar

- **Omfattande onboarding** (ONBOARDING.md)
- **AI-vägledning** (AI-GUIDANCE.md) för automatgenererad kod
- **Analyzers** som upprätthåller regler vid kompilering
- **Korrelationstester** som validerar semantisk korrekthet automatiskt
- **Documentation-first** (ADR-index + bilagor)

---

## Omfång

### Ingår

Detta ADR gäller för:

- All ny utveckling inom VGR:s domänprojekt
- Use cases i applikationslagret
- Infrastrukturens frågeöversättning
- Domänmodellering och semantisk registrering
- AI-assisterad kodgenerering

### Ingår inte

Detta ADR reglerar **inte**:

- UI-ramverk (React, Blazor, etc.)
- Integrationsmönster för externa system
- Deployments (Kubernetes, Docker, etc.)
- Specifik CI/CD-miljö

---

## Valideringskriterier

Detta arkitekturbeslut är framgångsrikt när:

1. ✅ Nya utvecklare kan navigera kodbasen via domänspråk inom 1 vecka
2. ✅ 95%+ av queries pushas ned till SQL (telemetrimätt)
3. ✅ AI-verktyg genererar semantiskt korrekt kod utan manuell korrigering
4. ✅ Domänförändringar propagieras säkert via refaktoreringsverktyg
5. ✅ Korrelationstester har 100% pass-rate i CI/CD
6. ✅ p95-frågetid är < 50 ms för indexerade semantiska queries

---

## Efterlevnad

Alla kodbidrag måste:

- [ ] Placera nya domänbegrepp i `VGR.Domain`
- [ ] Uttrycka queries via semantiska mönster (APPENDIX H)
- [ ] Tillhandahålla expansions för nya domänmetoder som kräver EF-översättning
- [ ] Inkludera korrelationstester som validerar SQL-översättning
- [ ] Använda typade projektioner i `Domain.Queries` (inga anonyma typer)
- [ ] Dokumentera semantiska attribut för registry-detektering
- [ ] Följa namngivningskonventioner (semantiska, inte tekniska suffix)

Avvikelser flaggas av analyzers vid kompilering.

---

## Referenser

### Grunddokument

- **ARCHITECTURE-CANON.md** – Fullständig arkitekturspecifikation
- **ARCHITECTURE-NAME.md** – Namnlogik (E-Clean vs Semantic Architecture)
- **ARCHITECTURE-WHY.md** – Filosofisk grund
- **ONBOARDING.md** – Introduktion för utvecklare
- **AI-GUIDANCE.md** – Instruktioner för AI-assistenter

### Bilagor

- **APPENDIX B** – Designprinciper för Semantic Architecture
- **APPENDIX C** – Semantiska komponenter
- **APPENDIX D** – Verktygsintegration & Roslyns semantiska modell
- **APPENDIX E** – Jämförelse med Clean Architecture & DDD
- **APPENDIX F** – AI-assisterad utveckling
- **APPENDIX G** – Specifikation för Semantic Registry
- **APPENDIX H** – Semantiska frågemönster
- **APPENDIX I** – Regler för Semantic Expansion
- **APPENDIX J** – Prestanda & frågeoptimering

### Relaterade ADR:er

- **ADR-001** – Indexpolicy (databasprestanda)
- **ADR-002** – Semantiska namn för tester (kodergonomi)
- **ADR-003** – Domänens semantiska felvokabulär
- **ADR-004** – Semantisk precision i undantagsfabriker
- **ADR-005** – Felsemantik är verifierbar domänbeteende
- **ADR-006** – Delivery översätter domänens felsemantik till transportsemantik
- **ADR-007** – Dubbel felkanal: domänundantag och Utfall
- **ADR-008** – Värdeobjektskontrakt, semantiska tolknings-API:er och odefinierade domänoperationer
- **ADR-009** – Produktionshärdning av delivery- och infrastrukturlager
- **ADR-010** – Persistenslagret uttrycker relationella garantier för systeminvarianter

---

## Versionshistorik

| Version | Datum       | Förändringar                        | Författare            |
|---------|-------------|--------------------------------------|------------------------|
| 1.0.0   | 2025-01-24  | Första formella deklarationen        | VGR Architecture       |
| 1.1.0   | 2026-04-03  | Integrerat ADR-000B: epistemisk separation, tolkningsram, verkställighetsstrategi. ADR-000B borttagen. | VGR Architecture |

---

## Slutsats

Epistemic Clean Architecture med Semantic Architecture som implementation innebär ett grundläggande skifte:

**Från:** Tekniska artefakter som styr strukturen  
**Till:** Domänspråket som styr strukturen

**Från:** Spridda epistemiska källor  
**Till:** Enad semantisk sanning

**Från:** Manuell översättningsbörda  
**Till:** Systematisk semantisk exekvering

**Från:** AI-opaka kodbaser  
**Till:** AI-native semantiska system

Detta beslut etablerar grunden för hållbara, evolverbara, högpresterande och mänskligt centrerade verksamhetssystem.

> *Language is the interface. Semantics execute.*

— **Epistemic Clean Architecture v1.0**
