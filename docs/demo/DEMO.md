# Demonstrationsinstruktion — E-Clean & Semantic Architecture

## Förberedelse

```bash
dotnet run --project src/VGR.Web/VGR.Web.csproj
```

Öppna webbläsaren på den URL som visas (normalt `http://localhost:5000`).

## Demoflöde

Demot följer ordningen på indexsidans knappar: **Diagram → Domän → API → Data**.
Varje steg bygger på föregående — börja med helhetsbilden, zooma in stegvis.

---

### 1. Diagram — helhetsbilden

**URL:** `/diagrams`

#### Lagerstruktur (koncentrisk)

Visa först den koncentriska vyn. Poängen:

- Domänen i centrum — **suverän**, inga beroenden utåt
- Semantik omsluter domänen — översätter domänspråket till SQL
- Applikation, Infrastruktur och Teknik som **sektioner i samma ring** — samma avstånd från kärnan. Det finns beroenden mellan dem (Applikation använder Infrastruktur och Teknik), men alla beror inåt på Domän/Semantik
- Leverans ytterst — tunnt skal

Klicka **Linjär** för att visa samma struktur som gruppboxar med bézier-beroendepilar.

#### Anropskedja

Visa hur ett anrop flödar genom lagren:

- Leverans: controller tar emot HTTP, översätter till domänspråk
- Applikation: interactor väljer verkställighetsstrategi (pushdown, selektiv hydrering)
- Förgrenar till Kärna (domänlogik, invarianter) och Semantik (frågöversättning)
- Semantik → Infrastruktur: EF Core översätter LINQ till SQL

#### Domänmodell

Visa klassdiagrammet. Poängen:

- Relationsflöde: Region → Person → Vårdval → Tidsrymd
- Färger: grönt = aggregat, blått = entitet, gult = värdeobjekt
- `*` markerar `[SemanticQuery]` — metoder som automatiskt översätts till SQL
- Klicka en ruta för att förstora — visa egenskaper och metoder i detalj

#### Semantiska expansioner

Visa kedjan:

```
Vårdval.ÄrAktivt → Tidsrymd.ÄrTillsvidare → Slut == null
```

Poängen: domänlogik definieras **en gång** i C#, översätts automatiskt till SQL.
Ingen duplicering, ingen drift.

---

### 2. Domän — strukturen i detalj

**URL:** `/domain`

Visa den reflektionsbaserade domänkatalogen:

- Expandera ett aggregat (Region eller Person) — visa egenskaper och metoder
- Peka på `[SemanticQuery]`-taggarna — dessa metoder kan pushas ned till SQL
- Peka på `readonly`-taggarna — domänen skyddar sig själv (VGR001/VGR002-analyzers)
- Visa `Throw`-klassen — semantiska undantagskoder (`Person.SlutFöreStart`)

---

### 3. API — transportlagret

**URL:** `/api`

Visa de automatiskt genererade API-endpoint-vyerna:

- Varje controller med sina routes, HTTP-metoder och parametrar
- DTO-fält med valideringsregler
- Statuskoder — domänfel mappas till RFC 9457 ProblemDetails

---

### 4. Data — interagera med domänen

**URL:** `/data`

Visa det reflektionsdrivna CRUD-gränssnittet:

- Lista aggregattyper → klicka för att se instanser
- Navigera relationer: Region → Person → Vårdval
- Skapa en ny entitet via formulär — domänens invarianter skyddar mot ogiltigt state
- Provocera ett domänfel (t.ex. överlappande vårdval) — visa ProblemDetails-svaret

---

## Nyckelbudskap

| Punkt | Var visas det |
|-------|--------------|
| Domänen är suverän | Koncentriska diagrammet |
| Logik definieras en gång | Semantiska expansioner |
| Systemet beskriver sig självt | Alla fyra System-UI-sidorna |
| Invarianter skyddar alltid | Data-sidan (provokation) |
| Inga externa beroenden i UI:t | Sidkällan (ingen `<script src="...">`) |

## Vanliga frågor under demo

**"Var definieras affärslogiken?"**
→ I domänlagret (VGR.Domain). Visa Region.cs eller Tidsrymd.cs.

**"Hur blir domänmetoder SQL?"**
→ Visa expansionsdiagrammet, sedan öppna TidsrymdExpansions.cs i Infrastructure.EF.

**"Vad händer vid fel?"**
→ Visa Data-sidan, provocera ett fel, visa ProblemDetails. Visa sedan DomainMappingExtensions.

**"Hur lägger man till en ny domäntyp?"**
→ Skapa typ i VGR.Domain → markera med [SemanticQuery] → kompilera → Roslyn-generatorn
säger till om expansion saknas → lägg till i Infrastructure.EF → klart. Syns automatiskt
i /domain, /data, /diagrams.
