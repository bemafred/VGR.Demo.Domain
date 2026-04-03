# ADR-001: Indexpolicy för semantiska frågor

## Status
Föreslagen

## Kontext

För att upprätthålla konsekvent prestanda och stödja pushdown-strategin behövs en explicit indexeringspolicy.
Målet är att varje `Domain Query (DQ)` som når databasen ska ha motsvarande index.

## Beslut

## Principer

1. **Primära index** på Id-fält (`RegionId`, `PersonId`, `VardvalId`).
2. **Sekundära index** på frekvent filtrerade kolumner, t.ex. `PersonId` i `Vardval`.
3. **Kombinerade index** när DQ filtrerar på flera kolumner (t.ex. `StartDatum`, `SlutDatum`).
4. **Uppföljning**: pushdown-tester kontrollerar att filtrering sker i SQL, inte i minnet.

## Exempel på DQ → Index

| Domain Query                 | Föreslaget index                                                                             |
|------------------------------|----------------------------------------------------------------------------------------------|
| `person.AllaVårdval()`       | `CREATE INDEX IX_Vardval_PersonId ON Vardval(PersonId)`                                      |
| `person.AktivtVårdval(date)` | `CREATE INDEX IX_Vardval_PersonId_Start_Slut ON Vardval(PersonId, StartDatum, SlutDatum)`    |

## Observability

- Mätning av query-latens (p95/p99).  
- Kontroll av `Rows materialized` per request.  
- Loggning av `Include(...)`-användning (bör vara ~0).

