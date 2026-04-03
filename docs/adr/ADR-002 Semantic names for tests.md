
# ADR-002: Semantiska namn för testprojekt

## Status
Accepterad

## Kontext
Traditionella .NET-projekt använder suffixet `.Tests` universellt för alla testprojekt.  
Detta är ett **tekniskt namn** som beskriver *hur* vi gör något (xUnit-ramverk), inte *vad* vi gör.

E-Clean strävar efter att **mening uttrycks i alla lager**, inklusive projektnamn.

## Beslut
Vi använder **semantiska suffix** för testprojekt:

- **`.Verifications`** – projekt som *verifierar* invarianter, regler eller integrationer
- **`.Correlations`** – projekt som *korrelerar* domänbeteende (in-memory) med SQL-resultat (EF)

## Konsekvenser

### Fördelar
- Namn uttrycker *mening*, inte bara teknik
- Tydligare navigerbarhet (utvecklare förstår omedelbart vad projektet gör)
- AI-verktyg kan resonera om "verifikationer" och "korrelationer" semantiskt
- Konsistent med E-Clean-principerna (Semantic Primacy, Code Ergonomics)

### Nackdelar
- Bryter med .NET-konvention (`.Tests`)
- Kräver förklaring för nya utvecklare (hanteras i ONBOARDING.md och KODERGONOMI.md)
- Verktyg som förväntar sig `.Tests` kan behöva konfigureras (t.ex. dotcover, NCrunch)

## Relaterade dokument
- `docs/guides/KODERGONOMI.md` – förklaring av semantiska namn som princip
- `docs/guides/PLACERING.md` – praktisk tillämpning i projektstrukturen
- `AI-GUIDANCE.md` – instruktion till AI-verktyg
