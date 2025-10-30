# VGR Arkitektur — ADR-Index

## Syfte

För att upprätthålla konsekvent prestanda och stödja pushdown-strategin används en explicit indexeringspolicy.
Målet är att varje `Domain Query (DQ)` som når databasen ska ha motsvarande index.

## Principer

1. **Primära index** på Id-fält (`RegionId`, `PersonId`, `VardvalId`).
2. **Sekundära index** på frekvent filtrerade kolumner, t.ex. `PersonId` i `Vardval`.
3. **Kombinerade index** när DQ filtrerar på flera kolumner (t.ex. `StartDatum`, `SlutDatum`).
4. **Uppföljning**: pushdown-tester kontrollerar att filtrering sker i SQL, inte i minnet.

## Exempel på DQ → Index

| Domain Query | Föreslaget index |
|—————|——————|
| `AllaVårdval(personId)` | `CREATE INDEX IX_Vardval_PersonId ON Vardval(PersonId)` |
| `GällandeVårdval(personId, date)` | `CREATE INDEX IX_Vardval_PersonId_Start_Slut ON Vardval(PersonId, StartDatum, SlutDatum)` |

## Observability

- Mätning av query-latens (p95/p99).  
- Kontroll av `Rows materialized` per request.  
- Loggning av `Include(...)`-användning (bör vara ~0).

