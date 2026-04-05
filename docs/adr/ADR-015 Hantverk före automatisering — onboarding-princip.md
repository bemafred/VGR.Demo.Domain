# ADR-015: Hantverk före automatisering — onboarding-princip

## Status
Föreslagen

## Kontext

AI-kodningsverktyg (Claude Code, Copilot, Cursor) gör det möjligt att producera arkitekturellt korrekt kod utan att förstå *varför* den ser ut som den gör. En junior kan generera en interactor, en controller och ett test som följer alla mönster — utan att ha internaliserat principerna bakom dem.

Det skapar en bräcklig situation: koden ser rätt ut, men utvecklaren kan inte felsöka, utvärdera alternativ eller resonera om avvikelser. När AI:n gör fel — och den kommer att göra fel — saknar utvecklaren verktygen att upptäcka det.

Roslyn-analyzers (VGR001–003) ger compile-time feedback som fångar strukturella brott. Men de är *mekaniska* — de säger *vad* som är fel, inte *varför*. En utvecklare som bara lärt sig följa analyzer-meddelanden har inte djupare förståelse än en som kopierar Stack Overflow-svar.

## Beslut

### Princip: hantverk före automatisering

Nya utvecklare i teamet ska genomgå en onboarding-fas där AI-kodningsverktyg **inte används** för domän- och arkitekturrelaterad kod. Fasen är tidsbegränsad och har tydliga utgångskriterier.

### Onboarding-fas

Under onboarding-fasen:

1. **Skriv för hand**: aggregat, värdeobjekt, interactors, controllers, EF-konfiguration — utan AI-generering
2. **Bryt analyzers medvetet**: skriv en public setter, se VGR001, förstå *varför* den finns
3. **Skriv ett test som misslyckas**: förstå vad SqliteHarness gör, varför ReadDbContext är NoTracking
4. **Läs ADR:er**: varje ADR som berörs av uppgiften ska vara läst och förstådd innan implementation
5. **Code review med mentor**: fokus på *resonemang*, inte bara kodkvalitet

### Utgångskriterier

Onboarding-fasen avslutas när utvecklaren kan:

- Förklara varför controllers inte får referera DbContext (inte bara "för att analyzern säger det")
- Skapa ett värdeobjekt med Tolka/FörsökTolka utan att titta på befintliga exempel
- Resonera om var ny kod hör hemma (domän vs application vs infrastructure)
- Identifiera när AI-genererad kod bryter mot arkitekturens principer

### Efter onboarding

AI-verktyg är fria att använda. Analyzers fungerar som säkerhetsnät. Code review skiftar från "lär dig mönstret" till "utvärdera lösningen".

## Konsekvenser

### Positiva
- **Djup förståelse**: utvecklare som kan resonera, inte bara reproducera
- **Robusthet**: teamet kan hantera situationer där AI-verktyg ger felaktiga förslag
- **Kultur**: hantverk värderas — verktyg är acceleratorer, inte ersättare

### Negativa
- **Långsammare onboarding initialt**: första veckorna producerar mindre kod
- **Kräver mentorskap**: senior-tid investeras i onboarding

### Neutrala
- Principen är organisatorisk, inte teknisk — den kan inte verkställas av en analyzer
- Gäller domän- och arkitekturrelaterad kod — verktygsstöd för boilerplate, konfiguration och infrastruktur är fritt även under onboarding
