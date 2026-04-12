# ADR-018: Självbeskrivande diagram via semantisk metadata

## Status
Genomförd

## Kontext

ADR-000 fastslår att domänens språk ska vara förstklassens medborgare genom hela stacken och att
systemet ska vara **maskinläsbart** för både människor och AI. ADR-012 realiserade detta genom
reflection-drivet `/data`-UI, `/domain`-struktur och `/api`-vy. Systemet kan redan *visa sig självt*
och *interagera med sig självt*.

Men det kan inte **rita sig självt**.

### Observerad lucka

Vid en demo med en junior utvecklare konstaterades att:

1. Solution-filens vertikala uppdelning var "mycket lättare att förstå än andra arkitekturer"
2. Koden var läsbar och navigerbar via domänspråket
3. **Visuell struktur saknades** — bilder underlättar initial förståelse

Traditionella lösningar (PowerPoint, Visio, handritade diagram i docs) har en fundamental brist:
**de divergerar från koden**. Dag 1 stämmer de, dag 30 är de vilseledande. De bryter mot
principen att koden är modellen.

### Befintlig infrastruktur

Allt som behövs för att generera korrekta diagram finns redan i runtime:

| Datakälla | Tillgänglig via | Innehåll |
|-----------|----------------|----------|
| **DomainModel** | `SemanticRegistry.GetModel()` | Typer, klassificering (Aggregate/Entity/ValueObject/Identity/...), egenskaper, metoder, `[SemanticQuery]`-markeringar |
| **EF-metadata** | `DbContext.Model` | Navigationer, kardinalitet, relationstyper |
| **Expansionsregister** | `SemanticRegistry.TryGet()` | `MethodInfo → LambdaExpression`-mappningar |
| **Assembly-referenser** | `Assembly.GetReferencedAssemblies()` | Beroendegrafen mellan projekt |

System-UI:ts befintliga mönster — `DomainPage`, `IndexPage`, `ApiPage` — genererar HTML som
rena C#-strängar från exakt dessa datakällor. En `DiagramPage` följer samma mönster.

## Problem

Arkitekturen saknar visuell självbeskrivning. Konsekvenser:

1. **Onboarding** — nya utvecklare måste bygga mentala modeller från kod, inte bilder
2. **Demo** — presentationer kräver externa, underhållskrävande diagram
3. **Epistemisk lucka** — systemet kan beskriva sin struktur i text men inte visuellt
4. **Stale documentation** — statiska diagram i docs/ blir inaktuella vid domänförändringar

## Beslut

### Princip: systemet ritar sig självt

Diagram genereras vid runtime från samma metadata som driver frågöversättning,
domän-UI och API-introspection. **Diagrammen är inte dokumentation om systemet —
de är systemet som beskriver sig självt.**

Detta är en direkt konsekvens av E-Clean: om domänens semantik är maskinläsbar,
är visuell rendering en projektion av den semantiken — inte en separat artefakt.

### 1. Ny sida: `/diagrams` i System-UI

`DiagramPage.cs` läggs till i `VGR.Technical.Web/SystemUI/` och registreras i
`DomainEndpoints.MapDomainEndpoints()`. Samma mönster som befintliga sidor:
statisk `Render()`-metod som returnerar en HTML-sträng.

### 2. Mermaid.js för klientsidesrendering

Servern genererar Mermaid-syntax (text) från metadata. Klienten renderar till SVG
via Mermaid.js. Inga byggsteg, inga npm-beroenden — ett `<script>`-tag.

Mermaid valdes framför alternativen:

| Alternativ | Avfört | Anledning |
|------------|--------|-----------|
| D3.js | Ja | Kräver omfattande JavaScript, orimlig komplexitet |
| Graphviz/viz.js | Ja | WASM-beroende, svagare stöd för klassdiagram |
| Server-side SVG | Ja | Kräver layoutalgoritm i C# |
| PowerPoint/Visio | Ja | Divergerar från kod, inte versionshanterat |
| Statisk Mermaid i docs/ | Ja | Går stale — bryter "kod är modellen" |

### 3. Tre diagramtyper från tre datakällor

#### Diagram 1: Domänmodell (classDiagram)

**Datakälla:** `DomainModel.Types` + EF-metadata (`GetNavigations()`).

Visar aggregat, entiteter och värdeobjekt med egenskaper, metoder och relationer.
Typer färgkodas efter `DomainTypeKind` — samma färgschema som `/domain`-sidan.
`[SemanticQuery]`-metoder markeras visuellt.

Relationer härleds genom:
- `Property.TypeName` som matchar annan `DomainType.Name` → referensnavigation
- `IReadOnlyList<T>` där T matchar `DomainType.Name` → samlingsnavigation
- EF-metadata ger kardinalitet (1:*, 1:1)

#### Diagram 2: Semantisk översättningskedja (flowchart)

**Datakälla:** `SemanticRegistry`s expansionsregister.

Visar hur `[SemanticQuery]`-metoder expanderas steg för steg till SQL-kompatibla
uttryck. Varje nod är ett expansionssteg; kedjan spåras genom att rekursivt
kontrollera om en expansion innehåller ytterligare semantiska anrop.

```
Vårdval.ÄrAktivt → Period.ÄrTillsvidare → Slut == null → WHERE Slut IS NULL
```

Detta visualiserar den mekanism som gör E-Clean distinkt: domänlogik som
automatiskt blir SQL utan duplicering.

#### Diagram 3: Vertikal lagerstruktur (flowchart TD)

**Datakälla:** `Assembly.GetReferencedAssemblies()`, filtrerat på `VGR.*`.

Visar beroendegrafen mellan projekt. Varje assembly är en nod; referenser
är riktade kanter. Lager grupperas visuellt (Core, Semantic, Application,
Infrastructure, Delivery, Technical).

### 4. Diagrammen är levande

Diagrammen genereras vid varje sidladdning. Lägger du till ett aggregat, en
`[SemanticQuery]`-metod eller ett projekt syns det automatiskt — utan att
någon uppdaterar dokumentation.

### 5. Export till statiska filer (sekundärt)

Som komplement kan `/diagrams` erbjuda nedladdning av genererad Mermaid-syntax
och renderad SVG. Detta möjliggör inkludering i presentationer och docs/ utan
att bryta mot principen: **originalet lever i koden, statiska kopior är
medvetna snapshots**.

## Konsekvenser

### Positiva

- **Noll underhåll** — diagram uppdateras automatiskt vid domänförändringar
- **Onboarding** — nya utvecklare ser strukturen visuellt innan de läser kod
- **Demo** — inga externa verktyg behövs, systemet presenterar sig självt
- **Epistemisk koherens** — visuell representation härledd från samma metadata
  som driver runtime-beteende. Diagrammen kan inte ljuga.
- **Arkitekturbevis** — att systemet kan rita sig självt visar att den semantiska
  metadatan är tillräckligt rik för att bära visuell projektion. Det är ett
  starkare bevis för maskinläsbarhet än textbaserad introspection.

### Negativa

- **Extern JavaScript-dependency** — Mermaid.js (~150 KB). Kan mitigeras genom
  inbäddning som embedded resource för offline-demo.
- **Layout-begränsningar** — Mermaids automatiska layout ger inte alltid optimal
  visuell balans. Acceptabelt för automatgenererade diagram.
- **Komplexitetströskel** — för stora domäner kan klassdiagrammet bli svårläst.
  Mitigeras genom filtrering per `DomainTypeKind` och interaktiva collapse/expand.

### Invariant

Befintliga System-UI-sidor (`/`, `/domain`, `/api`, `/data`) påverkas inte.
`/diagrams` existerar parallellt och ersätter inget.

## Avgränsning

- **Interaktivitet** (klickbara noder som navigerar till `/data`-routes) är ett
  naturligt nästa steg men ingår inte i denna ADR.
- **Sekvensdiagram** (begäransflöde genom lager) kräver runtime-tracing och
  behandlas separat.
- **RDF/Turtle-export** av diagramstruktur (ADR-000, princip 5) är kompatibelt
  men ingår inte här.

## Implementationsstatus

- [x] Steg 1: `DiagramPage.cs` med Mermaid.js-integration och grundlayout
- [x] Steg 2: Domänmodelldiagram (classDiagram) från `DomainModel` + EF-metadata
- [x] Steg 3: Semantisk expansionskedja (flowchart) från expansionsregistret
- [x] Steg 4: Vertikal lagerstruktur (flowchart) från assembly-referenser
- [x] Steg 5: Route-registrering i `DomainEndpoints`, navigering från indexsidan

## Relaterade dokument

- ADR-000 — E-Clean & Semantic Architecture (princip 5: domänkatalog, princip 6: förklarbarhet)
- ADR-012 — Automatiskt domän-UI via reflection (samma mönster, textbaserad)
- ADR-015 — Hantverk före automatisering (diagram som pedagogiskt verktyg)
- ADR-016 — Dokumentationens epistemiska baslinje (levande diagram vs stale docs)
