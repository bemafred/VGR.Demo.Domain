# ADR-001: Indexpolicy för semantiska frågor

## Status
Genomförd

## Kontext

För att upprätthålla konsekvent prestanda och stödja pushdown-strategin behövs en explicit indexeringspolicy.
Målet är att varje semantisk fråga som når databasen ska ha motsvarande index, och att korrelationstester verifierar att filtrering sker i SQL, inte i minnet.

## Beslut

### Principer

1. **Primära index** på Id-fält (`RegionId`, `PersonId`, `VårdvalId`) — EF-konvention via `HasKey`.
2. **Sekundära index** på frekvent filtrerade kolumner, t.ex. `PersonId` i `Vårdval`.
3. **Kombinerade index** när frågor filtrerar på flera kolumner.
4. **Unika index** där domänen kräver unikhet under samtidighet (se ADR-010).
5. **Filtrerade index** för partiella unikhetsregler, t.ex. "högst ett aktivt vårdval per person".
6. **Korrelationstester** verifierar att domänmetoder och deras SQL-expansion ger samma resultat.

### Implementerade index

| Tabell | Index | Typ | Motivering |
|--------|-------|-----|------------|
| Person | `(RegionId, Personnummer)` | Unikt | Personnummer unikt per region (ADR-010 §4) |
| Vårdval | `(PersonId, EnhetsHsaId)` | Icke-unikt | Uppslag vid skapande och frågor |
| Vårdval | `(PersonId) WHERE Slut IS NULL` | Filtrerat unikt | Högst ett aktivt vårdval per person (ADR-010 §4) |

### Korrelationstester

Pushdown-verifiering sker via `VGR.Semantics.Linq.Correlations`:

| Domänmetod | Expansion | Verifierat |
|------------|-----------|:---:|
| `Tidsrymd.Innehåller(tidpunkt)` | `Start <= t AND (Slut IS NULL OR t < Slut)` | Ja |
| `Tidsrymd.Överlappar(annan)` | Halvöppen intervallöverlapp med NULL-hantering | Ja |
| `Tidsrymd.ÄrTillsvidare` | `Slut IS NULL` | Ja |
| `Vårdval.ÄrAktivt` | Delegerar till `Period.ÄrTillsvidare` | Ja |

## Observability (framtida mål)

Följande mätvärden ska implementeras när produktionsprovider väljs:

- Mätning av query-latens (p95/p99)
- Kontroll av materialiserade rader per request
- Verifiering att `Include(...)` inte används (pushdown som norm)

## Relaterade dokument

- `docs/adr/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/adr/ADR-010 Persistenslagret uttrycker relationella garantier för systeminvarianter.md`
- `docs/appendix/APPENDIX J - Performance & Query Optimization.md`
