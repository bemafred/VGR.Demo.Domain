# Kodergonomi – den mänskliga sidan av arkitektur

Kodergonomi handlar om **hur koden känns** att arbeta med.  
Det är en disciplin som balanserar teknik, språk och mänsklig perception.  
När koden är ergonomisk följer tanken med utan motstånd – och utvecklaren kan arbeta i rytm, inte i kamp.

## Syfte

Kodergonomi är inte en stilfråga, utan en **arbetsmiljöprincip**.  
Målet är att skapa kod som:

- Stödjer fokus och flyt  
- Är förutsägbar att navigera  
- Förklarar sig själv genom sitt språk  
- Förblir vacker även efter refaktorering  

## Kodergonomins pelare

### 1. Navigerbarhet
Koden ska vara lätt att röra sig i.  
IntelliSense, “Go to Definition” och symbolflöden ska spegla hur en människa tänker.  
Navigering får aldrig kräva kognitiv översättning – allt ska vara direkt och intuitivt.

### 2. Förutsägbarhet
Namngivning, konventioner och struktur ska följa ett mönster.  
Utvecklaren ska känna: *“jag tror det heter så här”* – och ha rätt.

### 3. Läsbar rytm
Kod är inte bara instruktioner – det är text.  
Den ska kunna läsas högt, förstås i rörelse och kännas harmonisk.  
Andningsrum, radbrytningar och balans skapar rytm.

### 4. Mentalt tempo
En ergonomisk kodbas låter utvecklaren hålla sitt tempo.  
Varje API, metod och klass stöder en naturlig tanke-hastighet.  
Allt som bryter flödet måste ifrågasättas.

### 5. Kognitiv friktionsfrihet
Avstå från onödiga lager, abstraktioner och förkortningar.  
Koden ska uppmuntra till förståelse – inte till tolkning.  
Låt det enklaste alltid vara det mest sanningsenliga.

### 6. Reflektiv återkoppling
Verktygen är en del av ergonomin.  
Kompilatorn, IDE:n och analyzers ska ge vägledning, inte bestraffning.  
Bra feedback hjälper tanken framåt.

### 7. Domänspråkets närvaro
Domänen ska höras i koden.  
När man läser `person.SkapaVårdval()` ska man känna igen verksamheten, inte tekniken.  
Det är **språklig ergonomi** – där kod blir förståelse.

### 8. Harmoni mellan verktyg och tanke
Ergonomin sitter lika mycket i miljön som i syntaxen.  
JetBrains Rider, Visual Studio och analyserna ska förstärka arkitekturen, inte styra den.  
Koden är ett instrument – IDE:n är dess resonanslåda.

---

## Semantiska namn – mening före teknik

E-Clean strävar efter att **mening uttrycks i alla lager**, även i namngivning.  
Därför använder vi **semantiska namn** som uttrycker *vad vi gör*, inte *hur vi kategoriserar*.

### Testprojekt: Verifications & Correlations

Traditionella .NET-projekt använder suffixet `.Tests` universellt.  
Vi bryter med detta och använder **semantiskt meningsfulla suffix**:

| Traditionellt namn         | Semantiskt namn                        | Vad det uttrycker                                                  |
|----------------------------|----------------------------------------|--------------------------------------------------------------------|
| `VGR.Domain.Tests`         | **`VGR.Domain.Verifications`**         | Vi *verifierar* att domänen uppfyller sina invarianter och regler. |
| `VGR.Semantics.Linq.Tests` | **`VGR.Semantics.Linq.Verifications`** | Vi *verifierar* att semantisk översättning fungerar korrekt.       |
| *(saknas traditionellt)*   | **`VGR.Semantics.Linq.Correlations`**  | Vi *korrelerar* domänmetoder (in-memory) med SQL-resultat (EF).    |
| `VGR.Web.Tests`            | **`VGR.Web.Verifications`**            | Vi *verifierar* end-to-end-flöden från HTTP till databas.          |

### Varför inte bara "Tests"?

1. **Mening före teknik**  
   "Tests" säger *hur* vi gör något (xUnit, NUnit, testramverk).  
   "Verifications" och "Correlations" säger *vad* vi gör (verifiera invarianter, korrelera beteenden).

2. **Navigerbarhet**  
   När en utvecklare ser `*.Verifications` vet hen: *"Här verifieras något viktigt."*  
   När hen ser `*.Correlations` vet hen: *"Här kollar vi att domän och SQL matchar."*

3. **AI-förståelse**  
   AI-verktyg (Sky, James, Copilot) kan **resonera om verifikationer** och **förstå korrelationer** – inte bara "köra tester".

4. **Kodergonomi**  
   Semantiska namn förstärker arkitekturens epistemiska natur – **kod som uttrycker kunskap**.

### Konvention

Vi använder:
- **`.Verifications`** för projekt som verifierar beteende, invarianter eller integrationer
- **`.Correlations`** för projekt som korrelerar domänsemantik mot SQL/EF-översättning

Denna konvention är **inte teknik** – den är **mening**.

---

## Kodergonomi i praktiken

- **Rider-profiler** och formatteringsregler ska spegla domänens rytm.  
- **Analyzers** hjälper till att upprätthålla läsbarhet och riktning.  
- **Review-kultur** fokuserar på *flöde och läsbarhet* snarare än semikolon.  
- **Navigerbarhet** testas lika noggrant som prestanda.

Kodergonomi är en långsiktig investering i kreativitet, kvalitet och fokus.  
När koden känns rätt, **blir** den rätt.