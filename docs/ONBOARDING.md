# ONBOARDING till E-Clean & Semantic Architecture

Välkommen! 🎉  
Den här koden kan se avancerad ut vid första anblicken – men syftet är det motsatta:  
att göra **din vardag enklare, tryggare och mer ergonomisk**.

Den här guiden förklarar:

1. Hur lösningen är uppbyggd (E-Clean + Semantik)
2. Vad du **vanligtvis behöver jobba med**
3. Vad som är **avancerad infrastruktur** som du sällan (eller aldrig) behöver röra
4. Hur du stegvis kan lära dig mer, t.ex. om `Expression`-träd

Målet är att du ska känna:  
> “Jag fattar var jag ska vara. Jag kan bygga ny funktionalitet utan att förstå all magi.”

---

## 1. Den stora bilden

Lösningen följer en variant av Clean Architecture, kallad **E-Clean**, där E står för **Epistemic/Semantic**.  
Grov struktur (förenklad, anpassa efter faktisk lösning):

```text
src/
├── VGR.Domain/              → Verksamhetsdomän: aggregat, VO:s, invariants, Throw
├── VGR.Domain.Queries/      → Domännära projektioner och queries
├── VGR.Technical/           → Teknisk domän: Outcome, tekniska abstraktioner
├── VGR.Application/         → Interaktorer (kommandon och queries)
├── VGR.Infrastructure.EF/   → Entity Framework konfiguration och DbContexts
├── VGR.Semantics...         → Attribut + semantik-lager för queries
├── VGR.Semantics.Generator/ → Source generator som producerar expansionskod
├── VGR.Tests/               → End-to-end tester (ofta SQLite in-memory)
└── VGR.Domain.Tests/        → Enhetstester av domänen
```

**Justera projektnamn och struktur i listan ovan** så att den exakt matchar aktuell lösning.

Grov ansvarsfördelning:

- **Domänen** (t.ex. `VGR.Domain`, `VGR.Domain.Queries`)  
  Här bor verksamhetens begrepp: aggregat, värdeobjekt, regler, invariants och domännära queries.

- **Applikation** (t.ex. `VGR.Application`)  
  Use-cases: kommandon och queries som använder domänen.

- **Semantik** (t.ex. `VGR.Semantics.*`)  
  - Attribut som markerar semantiska queries/expansioner.  
  - Semantiskt lager för queries över domänen (expansioner, registry).  
  - Source generator som kan generera delar av semantiken.

- **Infrastruktur** (t.ex. `VGR.Infrastructure.EF`)  
  EF, DbContext, migrations, tekniska integrationer.

Poängen: domänen är **rik men ren** – den blandas inte ihop med EF-detaljer.  
Semantiklagret står **ovanpå** domänen och hjälper EF/infrastruktur att förstå den.

---

## 2. Vad du oftast behöver jobba med

I en vanlig arbetsdag, när du implementerar ny funktionalitet, är det framför allt:

1. **Domänen** (t.ex. `VGR.Domain`)
   - Skapa/ändra aggregat.
   - Skapa/ändra värdeobjekt (t.ex. tidsrelaterade VO, id-typer, etc.).
   - Införa/justera invariants och domänlogik.
   - Läsa domännära metoder som t.ex. ”ÄrAktivt(...)” på rätt typ.

2. **Domännära queries** (t.ex. `VGR.Domain.Queries`)
   - Bygga projektioner och queries som är naturliga för domänen.  
   - Exempel: ”Alla aktiva vårdval för ett visst datumintervall”.  
   - Dessa går att förstå även utan EF-kunskap.

3. **Applikationslagret** (t.ex. `VGR.Application`)
   - Implementera interaktorer (CQRS): kommandon och queries.
   - Anropa domänen och/eller domänqueries.
   - Hantera `Outcome`/resultat och översätta till t.ex. HTTP-svar.

4. **Tester** (`VGR.Tests`, `VGR.Domain.Tests`)
   - Säkerställa att domänen beter sig korrekt.
   - Köra end-to-end flöden med test-databas (ofta SQLite in-memory).

Du kan komma långt bara genom att:

- Följa befintliga mönster i domänen och domänqueries.
- Kika på hur interaktorer i applikationslagret är uppbyggda.
- Köra testerna och se hur flödet ser ut från ”ytterkant” till domän.

---

## 3. Vad som är ”avancerat” (och oftast kan lämnas ifred)

Följande kod är mer ramverks- och infrastruktur-nära. Du behöver inte förstå allt från start.

### 3.1 Semantiska attribut (t.ex. `VGR.Semantics` / `VGR.Semantics.Abstractions`)

Här finns attribut och kontrakt för semantiken, t.ex.:

- `SemanticQueryAttribute`
- `ExpansionForAttribute`

Du kan tänka på dem som domänetiketter som:

- Markerar att en metod/projektion är semantiskt viktig.
- Används av generatorn och semantiklagret för att forma ett mer ergonomiskt API.

Du kommer ibland att **använda attributen i domänen**, t.ex. genom att dekorera en metod.  
Själva implementationen av attributen behöver du normalt inte röra.

### 3.2 Semantiskt lager för queries (t.ex. `VGR.Semantics.Queries`)

Typiskt innehåll:

- Ett ”semantic registry” där expansioner registreras.
- Extension-metoder som kopplar domännära begrepp till EF-vänliga `Expression<Func<...>>`.
- Kod som generatorn skriver ut.

Detta lager fungerar som ett **semantiskt adapterlager** ovanpå EF:

- EF får uttryck den kan översätta till SQL.
- Domän och applikation får ett språkligt, ergonomiskt API med Intellisense-stöd.

Ditt fokus här, när du väl rör detta lager, är att:

- Följa befintliga mönster när du lägger till ny semantik.
- Inte skapa alternativa, parallella semantik-vägar – håll dig till registry/generator-spåret.

### 3.3 Source generator (t.ex. `VGR.Semantics.Generator`)

Source generatorn:

- Läser attribut i domänen.
- Producerar kod till semantik-projektet (expansioner, registreringar).
- Hjälper till att undvika manuell copy/paste.

De flesta utvecklare kan behandla generatorn som ett ramverk:

- Du behöver veta *att* den finns och vad den ungefär gör.
- Du behöver sällan ändra dess internlogik.
- Vid förändringar följer du existerande mönster och gör det gärna tillsammans med en mer erfaren kollega.

---

## 4. Exempelidé: tidsrelaterade värdeobjekt och semantiska queries

Ett typiskt exempel i den här arkitekturen:

- Domänen har ett värdeobjekt för tidsintervall (t.ex. en `Tidsrymd`/”TimeRange”).
- Aggregat har metoder med tydliga namn, t.ex. ”ÄrAktivt(Tidsrymd)”.
- Domännära queries använder dessa typer/metoder direkt.

Problemet i EF-världen är ibland att:

- EF inte förstår komplexa värdeobjekt och deras metoder direkt.
- Då behöver vi semantiska expansioner där semantiklagret:
  - Plockar isär VO:t (start, slut, inkl/exkl logik, etc.).
  - Skapar en EF-vänlig `Expression<Func<TEntity, bool>>`.
  - Registrerar det i semantic-registry.

För dig som utvecklare innebär det:

- I domänen använder du värdeobjekt och metoder på ett naturligt, verksamhetsnära sätt.
- I applikationslagret använder du semantiska extension-metoder som dyker upp i Intellisense.
- Semantiklagret ser till att EF får en korrekt översättning utan att du behöver skriva SQL-liknande kod.

---

## 5. Tips om `Expression`-träd (för den som vill fördjupa sig)

En del kod – framför allt i semantiklagret – använder `Expression<Func<...>>`.  
Det är ett sätt att beskriva kod som data, så att EF kan översätta den till SQL.

Du *måste inte* kunna detta på djupet för att vara effektiv i vardagen, men om du vill lära dig:

1. Leta upp befintliga exempel i semantiklagret och följ mönstret:
   - Hur skapas `Expression<Func<TEntity, bool>>`?
   - Hur kombineras expressions (t.ex. med `Expression.AndAlso`)?

2. Testa i en ”sandlåda”:
   - Skriv små metoder som bygger expression-träd.
   - Inspektera `.ToString()` och se hur uttrycken ser ut.

3. Håll isär begreppen:
   - **Domänmetoder** – vanliga C#-metoder som körs direkt.
   - **Expressions** – beskrivningar av logik som EF ska översätta.

Målet med semantiklagret är att *du* ska kunna tänka i domänspråk, medan ramverket sköter översättningen.

---

## 6. Riktlinjer för juniora utvecklare

Om du är ny i koden:

1. **Börja i domänen**
   - Läs aggregat och värdeobjekt.
   - Identifiera vad som är viktigt för verksamheten.
   - Ignorera expressions & generatorer tills vidare.

2. **Läs domännära queries**
   - Fokusera på metoderna och deras intention: *vad* hämtas/beräknas?
   - Följ existerande mönster när du lägger till en ny query.

3. **Gå vidare till applikationslagret**
   - Se hur interaktorer använder domänen.
   - Bygg en ny use-case genom att följa en befintlig.

4. **När du behöver EF**
   - Använd befintliga mapping-konfigurationer som mall.
   - Fråga innan du introducerar helt nya mönster.

5. **Lämna semantiklagret ifred i början**
   - Om något ser ut som ”Expression magic” – betrakta det som ramverk.
   - Fråga en mer erfaren kollega om du är osäker.

---

## 7. Riktlinjer för seniora utvecklare

Om du är mer erfaren och vill arbeta med arkitektur/semantik:

1. Din spelplan är större:
   - Domän + domänqueries.
   - Applikationslager.
   - Semantik-attribut och semantiklager.
   - I vissa fall även source generatorn.

2. När du inför nya värdeobjekt:
   - Håll API:t tydligt och minimalt.
   - Avgör vilka metoder som ska vara semantiskt synliga (och annotera v.b.).

3. När EF inte kan översätta din domänlogik:
   - Lös det i semantiklagret, inte genom att duplicera EF-specifika metoder i domänen.
   - Registrera semantiken i registry och/eller via generatorn.
   - Håll applikationslagret rent från EF-workarounds.

4. Håll fast vid principen:
   > ”Applikationslagret ska använda domänen – inte EF-tricks.”

5. Tillämpa **Simplicity First**:
   - Om ett mönster kan förenklas utan att förlora semantisk styrka, förenkla.
   - Undvik onödiga abstraktioner; de flesta ska kunna följa koden utan att läsa tre lager helpers.

---

## 8. Testning

Vi har två huvudsakliga testnivåer (anpassa till faktisk lösning):

1. **Domäntester** (t.ex. `VGR.Domain.Tests`)
   - Verifierar regler, invariants och värdeobjekt.
   - Snabba, oberoende av databaser.

2. **End-to-end tester** (t.ex. `VGR.Tests`)
   - Kör applikationsflöden mot test-databas.
   - Verifierar att domän + semantik + EF + applikation samarbetar korrekt.

När du ändrar semantik, EF-mapping eller expression-träd, se till att:

- Befintliga tester fortsätter att gå igenom.
- Du kompletterar med nya tester där det är motiverat.

---

## 9. Frågor & vidare läsning

Om något känns oklart:

- Börja med att hitta ett liknande exempel i koden och följ mönstret.
- Prata med teamet – arkitekturen är byggd för att vara **diskuterbar**, inte mystisk.
- När du vill fördjupa dig:
  - Titta i semantiklagret och hur registry/generator används.
  - Läs på om `Expression<Func<T, bool>>` och hur EF översätter LINQ till SQL.

Målet är att alla i teamet – juniora som seniora – ska kunna:

- Jobba tryggt i domänen.
- Uppleva att semantiklagret **hjälper** snarare än stjälper.
- Veta att den avancerade koden finns där för att göra vardagen enklare.

Välkommen in i E-Clean & Semantic Architecture. 💙
