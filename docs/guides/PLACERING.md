# VGR Arkitektur — PLACERING

***Detta dokument beskriver hur de olika projekten i lösningen är organiserade och vilken roll de spelar i helheten.***

## Namngivningskonvention: Semantik före teknik

E-Clean använder **semantiska namn** som uttrycker *mening*, inte bara *teknik*.

Därför använder vi **`.Verifications`** och **`.Correlations`** istället för generiska **`.Tests`**:

| Suffix                | Betydelse                                                                              | Exempel                                             |
|-----------------------|----------------------------------------------------------------------------------------|-----------------------------------------------------|
| **`.Verifications`**  | Projekt som *verifierar* att regler, invarianter eller integrationer fungerar korrekt. | `VGR.Domain.Verifications`, `VGR.Web.Verifications` |
| **`.Correlations`**   | Projekt som *korrelerar* domänbeteende (in-memory) med SQL-resultat (EF).              | `VGR.Semantics.Linq.Correlations`                   |

**Motivering:**  
"Tests" är ett tekniskt ord som beskriver *hur* vi gör något (xUnit-ramverk).  
"Verifications" och "Correlations" är **semantiska ord** som beskriver *vad* vi gör (verifiera regler, korrelera beteenden).

Detta är **kod-ergonomi i praktiken** – namn som bär mening genom hela stacken.

---

## Struktur (per solution folder)

| Solution folder                        | Projekt                            | Syfte                                                                                                       |
|----------------------------------------|------------------------------------|-------------------------------------------------------------------------------------------------------------|
| **Core Domain**                        | `VGR.Domain`                       | Verksamhetsdomän: aggregat, värdeobjekt, invariants, `Throw`.                                               |
|                                        | `VGR.Domain.Queries`               | Domännära queries/predikat (utan EF-beroende).                                                              |
|                                        | `VGR.Domain.Verifications`         | Enhetstester av domänen och domän-queries (utan infrastruktur).                                             |
| **Application (UseCases)**             | `VGR.Application`                  | Interaktorer (kommandon och queries) som orkestrerar domän + infrastruktur.                                 |
|                                        | `VGR.Application.Stories`          | **Stories**: BDD-liknande verifiering av interaktorer och användningsfall.                                   |
| **Semantic Core**                      | `VGR.Semantics.Abstractions`       | Attribut och kontrakt för semantiska queries (`SemanticQueryAttribute`, `ExpansionForAttribute`).           |
|                                        | `VGR.Semantics.Linq`               | Query-provider + expression-rewriter (`WithSemantics`, `SemanticRegistry`) för domän→EF-LINQ.               |
|                                        | `VGR.Semantics.Generator`          | Source generator som bygger upp semantik-registret vid compile-time.                                        |
|                                        | `VGR.Semantics.Linq.Verifications` | **Verifiering** av semantisk översättning och query-beteende.                                               |
|                                        | `VGR.Semantics.Linq.Correlations`  | **Korrelation** av domänmetoder mot SQL via SQLite in-memory.                                               |
| **Infrastructure (Persistence & IO)**  | `VGR.Infrastructure.EF`            | Entity Framework-konfiguration och `DbContext` (Read/Write, pushdown-strategi).                             |
|                                        | `VGR.Infrastructure.Diagnostics`   | Diagnostik för EF-beteende och DbContext.                                                                   |
|                                        | `VGR.Infrastructure.PostgreSQL.Verifications` | **Verifiering** av providerspecifikt beteende för PostgreSQL.                                      |
|                                        | `VGR.Infrastructure.SqlServer.Verifications`  | **Verifiering** av providerspecifikt beteende för SQL Server.                                      |
| **Delivery (API & Hosting)**           | `VGR.Web`                          | ASP.NET Core-API, controllers, hosting.                                                                     |
|                                        | `VGR.Web.Verifications`            | **Verifiering** end-to-end: interaktorer och webb (SQLite in-memory).                                       |
| **Technical Domain**                   | `VGR.Technical`                    | Teknisk domän: `Utfall`, `Map`, `IClock`, intern infrastruktur för interaktorer.                            |
|                                        | `VGR.Technical.Testing`            | **Testinfrastruktur**: `SqliteHarness` för unified in-memory EF-testning (Read/Write DbContexts).           |
|                                        | `VGR.Technical.Verifications`      | **Verifiering** av teknisk domän och infrastruktur.                                                         |
| **Quality & Guardrails**               | `VGR.Analyzers`                    | Roslyn-analyzers som upprätthåller domänregler.                                                             |
| **Architecture & Docs**                | `docs/*`                           | Arkitektur- och policy-dokumentation (`ANALYS`, `PLACERING`, `POLICY`, `KODERGONOMI`, m.fl.).               |

---

## Appendices – Arkitektonisk Djupdykning

Appendixen (A–J) utgör den **epistemiska och teoretiska grunden** för arkitekturen. De förklarar *varför* arkitekturen är som den är, inte bara *hur* den fungerar.

### Översikt: Vad varje appendix innehåller

| Appendix | Titel | Vad den förklarar |
|----------|-------|-------------------|
| **A** | **Platform Dependence** | Varför arkitekturen är C#/.NET-specifik och varför det är en styrka. |
| **B** | **Design Principles of Semantic Architecture** | De sju grundprinciperna som formar arkitekturen (Semantic Primacy, Epistemic Cleanliness, m.fl.). |
| **C** | **Semantic Components** | Formell definition av Semantic Components – vad de är, vad de inte är, och hur de skiljer sig från microservices/bounded contexts. |
| **D** | **Tooling Integration & The Roslyn Semantic Model** | Hur Roslyn, IDE-tooling och analyzers är integrerade i arkitekturen – inte som tillägg, utan som grundläggande mekanismer. |
| **E** | **Comparison with Clean Architecture & Domain-Driven Design** | Hur Semantic Architecture förhåller sig till Clean Architecture och DDD – evolution, inte revolution. |
| **F** | **AI-Assisted Development with Semantic Architecture** | Varför arkitekturen är AI-native och hur AI kan delta som utvecklare, inte bara som autocomplete. |
| **G** | **Semantic Registry Specification** | Formell specifikation av Semantic Registry – arkitekturens "semantiska ryggrad". |
| **H** | **Semantic Query Patterns** | De sju kanoniska mönstren för semantiska queries (Predicate, Projection, Composite, Expansion-Based, Temporal, Joined, Aggregation). |
| **I** | **Semantic Expansion Rules** | Hur expansionsregler fungerar – den mekanism som gör arkitekturen till en semantisk graf utan graph-databas. |
| **J** | **Performance & Query Optimization** | Prestandaprinciper, pushdown-strategi, indexeringspolicy och observability-riktlinjer. |

### Vem ska läsa vilken appendix?

| Målgrupp                           | Läs dessa appendices först                         |
|------------------------------------|----------------------------------------------------|
| **Ny utvecklare**                  | B (Principles), C (Components), H (Query Patterns) |
| **Arkitekt**                       | A (Platform), E (Comparison), G (Registry)         |
| **Senior utvecklare/Tech Lead**    | D (Tooling), I (Expansions), F (AI)                |
| **AI/Tooling-intresserad**         | D (Tooling), F (AI), G (Registry)                  |
| **DDD/Clean Architecture-erfaren** | E (Comparison), B (Principles), C (Components)     |

### Varför appendices och inte huvuddokument?

Appendixen innehåller **djup teoretisk motivering** som inte behövs för daglig utveckling, men som är avgörande för:
- Att förstå *varför* designbeslut fattades
- Att försvara arkitekturen mot alternativ
- Att utbilda nya team members i arkitekturens epistemologi
- Att säkerställa långsiktig koherens vid evolution

De är referensmaterial för **arkitektonisk försvarbarhet** – inte daglig arbetsdokumentation.

---

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
*varje “världsdels-mapp” (Core Domain, Semantic Core, Delivery, m.fl.) innehåller både kod och dess verifiering.*