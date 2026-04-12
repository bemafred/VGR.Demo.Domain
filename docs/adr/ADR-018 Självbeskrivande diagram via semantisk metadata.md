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
| **Expansionsregister** | `SemanticRegistry.GetExpansions()` | `MethodInfo → LambdaExpression`-mappningar |
| **Assembly-referencer** | `Assembly.GetReferencedAssemblies()` | Beroendegrafen mellan projekt |

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

### 2. Ren SVG-generering, inga externa beroenden

Diagram genereras server-side som inline SVG med `StringBuilder` — samma mönster
som all annan HTML-generering i System-UI. Inga externa JavaScript-beroenden.

#### Varför inte Mermaid.js?

Mermaid.js utvärderades initialt men avfördes under engineering-fasen:

| Problem | Konsekvens |
|---------|------------|
| `classDef`/`cssClass` ignoreras av `theme: 'dark'` i classDiagram | Inga färger — fyra misslyckade försök |
| Syntaxfel i v11.14.0 med `}:::styleName` och Unicode-klassnamn | Diagram renderar inte alls |
| Externt CDN-beroende (~150 KB JavaScript) | Bryter mot BCL-only-principen, ingen kontroll |
| Trial-and-error mot opak rendering | Gissningar istället för kunskap |

**Lärdomen:** Externa rendering-beroenden bryter mot semantisk suveränitet.
Om vi inte kontrollerar renderingspipelinen kan vi inte garantera visuell korrekthet.
Ren SVG-generering ger **deterministisk, offline-kapabel, beroendefri rendering**
med full kontroll över varje pixel.

#### Varför inte andra alternativ?

| Alternativ | Avfört | Anledning |
|------------|--------|-----------|
| D3.js | Ja | Externt JS-beroende, samma kontrollproblem som Mermaid |
| Graphviz/viz.js | Ja | WASM-beroende, runtime-opakt |
| PowerPoint/Visio | Ja | Divergerar från kod, inte versionshanterat |
| Statisk SVG i docs/ | Ja | Går stale — bryter "kod är modellen" |

### 3. Tre diagramtyper

#### Diagram 1: Lagerstruktur (koncentrisk + linjär)

**Datakälla:** `Assembly.GetReferencedAssemblies()`, filtrerat på `VGR.*`.

Två vyer med toggle i UI:t:

- **Koncentrisk** (default): Ringar med domänen i centrum. Jämbördiga lager (Applikation,
  Infrastruktur, Teknik) som sektioner i samma ring — visar att de inte beror på varandra.
  Lagernamn och projektnamn roterade längs tangenten. Beroendeflödet kommuniceras av
  ringstrukturen själv — inga pilar behövs.

- **Linjär**: Gruppboxar med projektboxar inuti. Jämbördiga lager sida vid sida,
  centrerade horisontellt. Bézier-kurvor för inter-lager-beroenden.

#### Diagram 2: Domänmodell

**Datakälla:** `DomainModel.Types` + EF-metadata (`GetNavigations()`).

Visar aggregat, entiteter och värdeobjekt med egenskaper, metoder och relationer.
Typer färgkodas efter `DomainTypeKind` — samma färgschema som `/domain`-sidan.
`[SemanticQuery]`-metoder markeras med `*`.

**Layout:** Relationsdriven BFS från rottyper — huvudkedjan horisontellt,
värdeobjekt under sin ägare. Ger naturligt flöde: Region → Person → Vårdval.

**Relationslinjer:** Ortogonala paths (`H → V → H`) med automatiskt kantpunktsval
(närmaste sida per box) och kardinalitet (`1:*`, `1:1`).

**Interaktivitet:** Klickbara klassrutor med expand-ikon. Popup visar 2x förstorad
vy av klassen, centrerad horisontellt med halvtransparent backdrop.

Relationer härleds genom:
- `Property.TypeName` som matchar annan `DomainType.Name` → referensnavigation
- `IReadOnlyList<T>` där T matchar `DomainType.Name` → samlingsnavigation
- EF-metadata ger kompletterande navigationer och kardinalitet

#### Diagram 3: Semantisk översättningskedja

**Datakälla:** `SemanticRegistry.GetExpansions()`.

Visar hur `[SemanticQuery]`-metoder expanderas steg för steg till SQL-kompatibla
uttryck. Kedjan spåras genom en `ExpressionVisitor` (`MethodCollector`) som
detekterar om en expansions body refererar andra expansioner.

```
Vårdval.ÄrAktivt → Tidsrymd.ÄrTillsvidare → Slut == null
```

### 4. Diagrammen är levande

Diagrammen genereras vid varje sidladdning. Lägger du till ett aggregat, en
`[SemanticQuery]`-metod eller ett projekt syns det automatiskt — utan att
någon uppdaterar dokumentation.

## Konsekvenser

### Positiva

- **Noll underhåll** — diagram uppdateras automatiskt vid domänförändringar
- **Noll beroenden** — ren SVG, ingen extern JavaScript, fungerar offline
- **Full kontroll** — varje pixel deterministisk, färger garanterat korrekta
- **Onboarding** — nya utvecklare ser strukturen visuellt innan de läser kod
- **Demo** — systemet presenterar sig självt, inga externa verktyg
- **Förfinbar** — SVG-generering kan iterativt förbättras utan att byta ramverk
- **Interaktivitet** — klickbar förstoring följer samma mönster som `/data`-routes
- **Arkitekturbevis** — att systemet kan rita sig självt visar att den semantiska
  metadatan är tillräckligt rik för att bära visuell projektion

### Negativa

- **Layoutalgoritm i C#** — kräver beräkningslogik för positionering och linjedragning.
  Acceptabelt: domänmodellen är liten och layouten är enkel (BFS + rutnät).
- **Komplexitetströskel** — för stora domäner kan klassdiagrammet bli svårläst.
  Mitigeras genom klickbar förstoring och framtida expand/collapse.

### Invariant

Befintliga System-UI-sidor (`/`, `/domain`, `/api`, `/data`) påverkas inte.
`/diagrams` existerar parallellt och ersätter inget.

## Avgränsning

- **Expand/collapse** i klassrutor (visa/dölj egenskaper/metoder) är ett
  naturligt nästa steg men ingår inte i denna iteration.
- **Sekvensdiagram** (begäransflöde genom lager) kräver runtime-tracing och
  behandlas separat.
- **RDF/Turtle-export** av diagramstruktur (ADR-000, princip 5) är kompatibelt
  men ingår inte här.

## Implementationsstatus

- [x] Steg 1: `DiagramPage.cs` med ren SVG-generering och route-registrering
- [x] Steg 2: Lagerstruktur — koncentrisk vy med sektionerade ringar
- [x] Steg 3: Lagerstruktur — linjär vy med centrerade jämbördiga lager
- [x] Steg 4: Lagerstruktur — toggle mellan koncentrisk och linjär
- [x] Steg 5: Domänmodell — BFS-layout, ortogonala relationslinjer, kardinalitet
- [x] Steg 6: Domänmodell — klickbar förstoring med popup
- [x] Steg 7: Semantisk expansionskedja med kedjedetektering
- [x] Steg 8: `SemanticRegistry.GetExpansions()` exponerar expansionsregistret

## Relaterade dokument

- ADR-000 — E-Clean & Semantic Architecture (princip 5: domänkatalog, princip 6: förklarbarhet)
- ADR-012 — Automatiskt domän-UI via reflection (samma mönster, textbaserad)
- ADR-015 — Hantverk före automatisering (diagram som pedagogiskt verktyg)
- ADR-016 — Dokumentationens epistemiska baslinje (levande diagram vs stale docs)
