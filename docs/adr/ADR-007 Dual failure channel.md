# ADR-007: Dubbel felkanal — domänundantag och Utfall

## Status
Genomförd

## Kontext

Domänen uttrycker invariantbrott via `DomainException`-hierarkin och `Throw`-fabriker (ADR-003).
Men inte alla fel är invariantbrott. I applikationslagret uppstår förväntade affärsfel som inte bryter mot domänens regler utan representerar normala alternativa utfall:

- "Personnummer redan registrerat" (dubblettcheck mot databasen)
- "Region saknas" (data-beroende tillstånd)
- Valideringsfel som upptäcks vid domänvärdeskonstruktion

Dessa fel är:
- **förväntade** — de inträffar under normal drift
- **hanterbara** — anroparen kan agera på dem
- **icke-exceptionella** — de representerar inte bruten invariant

Att uttrycka dessa via exceptions skapar semantisk förvirring: undantaget förlorar sin roll som signal om bruten invariant och blir i stället en kontrollflödesmekanik.

## Beslut

### 1. Domänlagret äger exceptions

Aggregat, entiteter och värdeobjekt uttrycker fel **uteslutande** via `Throw` och `DomainException`-hierarkin.
Domänlagret returnerar aldrig `Utfall<T>`. Domänen vet att en regel har brutits — det är en exception per definition.

### 2. Applikationslagret äger Utfall

Interaktorer returnerar `Utfall<T>` som primär resultattyp. Förväntade affärsfel uttrycks via `Utfall<T>.Fail(message)`:

```csharp
if (dubblett)
    return Utfall<PersonId>.Fail("Personnummer redan registrerat");
```

Domänundantag som kastas av domänobjekt inom interaktorn fångas **inte** av interaktorn — de propagerar uppåt till Delivery-lagret.

### 3. Delivery konsoliderar båda kanalerna

`DomainMappingExtensions.Map()` hanterar båda:
- `Utfall.Fail(...)` → HTTP 400 (förväntad affärsfel)
- `Utfall.Ok(...)` → HTTP 200
- `DomainException` → kontextberoende HTTP-status (se ADR-006)

### 4. Gränsdragning mellan kanalerna

| Egenskap | Exception (`Throw`) | `Utfall<T>.Fail` |
|----------|---------------------|-------------------|
| **Ägare** | Domänlagret | Applikationslagret |
| **Semantik** | Bruten invariant, ogiltigt tillstånd | Förväntat alternativt utfall |
| **Förväntad?** | Nej — ska aldrig ske i korrekt flöde | Ja — normalt driftutfall |
| **Bär domänkod?** | Ja (`DomainException.Code`) | Valfritt (`Utfall.Code`, ADR-009) |
| **Kontrollflöde** | Avbryter — exception propageras | Fortsätter — anroparen beslutar |

### 5. Utfall är inte en monad

`Utfall<T>` är medvetet minimal: `Ok(value)` / `Fail(error)`. Den saknar `Map`, `Bind` eller `Match`.
Detta är ett avsiktligt val: applikationslagrets interaktorer är platta, sekventiella flöden.
Kombinatorkedjor adderar komplexitet utan nytta i detta sammanhang.
Om behovet av kedjning uppstår ska det motiveras som en separat ADR.

## Konsekvenser

### Fördelar
- Tydlig ansvarsfördelning: domänen kastar, applikationen bedömer, delivery översätter.
- Exceptions behåller sin semantiska tyngd — de signalerar verkligt bruten invariant, inte normalt alternativflöde.
- Interaktorer uttrycker förväntade affärsutfall deklarativt utan try/catch-kaskader.
- Delivery-lagret kan ge klienten meningsfull feedback oavsett felkanal.

### Nackdelar
- Två felmönster kräver att utvecklare förstår gränsdragningen.
- ~~`Utfall<T>.Fail` bär ingen maskinläsbar kod~~ — Åtgärdad i ADR-009: `Fail` accepterar nu valfri `code`-parameter. Delivery bär `code` i `Extensions["code"]` om den finns.
- Domänundantag som kastas i interaktorer propagerar tyst — det är upp till Delivery att fånga dem korrekt (se ADR-006, kända avvikelser).

## Relaterade dokument
- `docs/adr/ADR-003 Domain failure vocabulary.md` — domänens undantagsvokabulär
- `docs/adr/ADR-006 Delivery translates domain failure semantics to transport semantics.md` — HTTP-översättning
- `docs/adr/ADR-000 E-Clean & Semantic Architecture.md` — arkitekturgrund
