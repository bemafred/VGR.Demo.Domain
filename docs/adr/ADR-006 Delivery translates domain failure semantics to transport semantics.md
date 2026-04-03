# ADR-006: Delivery översätter domänens felsemantik till transportsemantik

## Status
Genomförd

## Kontext
I E-Clean är domänfel domänfel.
De uttrycker bruten invariant, ogiltigt domänvärde, otillåten tillståndsövergång eller annan domänsignifikant mening.

I samma arkitektur är HTTP-fel transportfel.
De uttrycker hur en begäran och dess svar ska förstås i HTTP-lagret:
- statuskod
- problemtyp
- eventuella adapter-specifika felkoder
- kontrakt mot klient

Dessa två semantiska system är relaterade men inte identiska.
Om domänens undantagstyper mappas direkt och generellt till HTTP-statuskoder uppstår epistemisk kollaps:
- transportlagret låtsas att domänens begrepp och HTTP:s begrepp är samma sak
- samma domänfel kan kräva olika HTTP-svar i olika adapterkontexter
- domänens mening reduceras till transportklassificering

Samtidigt behöver Delivery-lagret kunna bära domänens mening vidare till klienten när det är relevant.

## Beslut

### 1. Domänfel förblir domänfel
`DomainException` och `Throw` uttrycker domänens felsemantik.
De är inte HTTP-koder, transportkoder eller API-felkoder.

### 2. Delivery äger transportsemantiken
HTTP-statuskod, `ProblemDetails.type`, titel och eventuella adapter-felkoder ägs av Delivery-lagret.
De definieras utifrån HTTP-begärans och HTTP-svarets mening, inte direkt utifrån domänens undantagstyp.

### 3. Domänens mening får bäras vidare
Delivery får bära information om domänfelet i svaret, exempelvis:
- domänens semantiska identifierare
- undantagstyp
- domänmeddelande
- relevanta domänbegrepp

Denna information är metadata om domänfelet, inte transportkod.

### 4. Direktmappning är tillåten endast vid fullständig epistemisk grund
En direkt mappning från ett domänfel till ett HTTP-svar får göras endast när mappningen är:
- semantiskt korrekt
- kontextstabil
- fullständig för den aktuella adapterytan
- dokumenterad
- verifierad

### 5. Ingen global typ-till-status-tabell utan epistemisk motivering
Följande är otillåtet som generell princip:
- `DomainInvariantViolationException => 409`
- `DomainValidationException => 400`
- `DomainAggregateNotFoundException => 404`

Sådana mönster får endast införas där deras mening är fullständigt underbyggd för den specifika HTTP-ytan.

## Implementationsnotering
- Den centrala HTTP-mappningen hålls i Delivery-lagret på en plats.
- Om implementationen växer delas den upp via `partial` classes eller interna funktionsenheter, inte via interface-abstraktioner utan semantisk vinst.

## Konsekvenser

### Fördelar
- Domänens semantik förblir primär.
- HTTP-lagret får korrekt eget ansvar.
- Samma domänfel kan uttryckas olika i olika adapters utan semantisk förvirring.
- Klienter kan få både korrekt HTTP-beteende och rik domäninformation.

### Nackdelar
- Delivery-lagret måste uttrycka mer explicit mappningslogik.
- Färre generella catch-all-mappningar.
- Kräver tydligare testning och dokumentation i adapters.

## Regler för implementation
- Domänlager får inte känna till HTTP.
- `DomainException.Code` är domänsemantik, inte HTTP-felkod.
- Adapter-felkoder, om sådana används, ska namnges i adapter-/delivery-rymden.
- `ProblemDetails.type` ska beskriva transportproblemets mening.
- Domänens undantagsinformation ska läggas i separata fält eller extensions, inte blandas ihop med transportkod.

## Tidigare kända avvikelser (åtgärdade)

Den ursprungliga implementationen hade två brister:

1. **Global typ-till-status-mappning utan epistemisk motivering** — endast `DomainInvariantViolationException` (409) och `DomainArgumentFormatException` (400) hanterades; övriga föll till 500.
2. **Ofullständig undantagshantering** — fem av sju `DomainException`-typer gav generiska 500-svar.

Åtgärd: pragmatisk default-mappning med epistemisk motivering per typ:

| Undantagstyp | HTTP | Motivering |
|---|---|---|
| `DomainArgumentFormatException` | 400 | Klientens indata har fel format |
| `DomainValidationException` | 422 | Semantiskt ogiltig domänindata |
| `DomainAggregateNotFoundException` | 404 | Efterfrågad resurs saknas |
| `DomainInvariantViolationException` | 409 | Tillståndskonflikt — begäran bryter invariant |
| `DomainInvalidStateTransitionException` | 409 | Tillståndskonflikt — otillåten övergång |
| `DomainConcurrencyConflictException` | 409 | Samtidighetskonflikt |
| `DomainIdempotencyViolationException` | 409 | Duplicerad begäran |
| `DomainUndefinedOperationException` | 422 | Operationen saknar mening för aktuellt tillstånd |

Infrastrukturella undantag (ADR-009):

| Undantagstyp | HTTP | Motivering |
|---|---|---|
| `OperationCanceledException` | 499 | Klienten avbröt begäran |
| `DbUpdateConcurrencyException` | 409 | Optimistisk samtidighetskonflikt |
| `DbUpdateException` | 422 | Databaskonstraint bruten |

Alla felsvar följer RFC 9457 med `Type` (URN), `Title`, `Status`, `Detail` och `Extensions["code"]` (för domänfel).

## Relaterade dokument
- `docs/adr/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/adr/ADR-003 Domain failure vocabulary.md`
- `docs/adr/ADR-004 Semantic precision in exception factories.md`
- `docs/adr/ADR-005 Verification of domain failure semantics.md`
- `docs/adr/ADR-007 Dual failure channel.md`
- `docs/adr/ADR-009 Produktionshärdning av delivery- och infrastrukturlager.md`
- `docs/guides/POLICY.md`
