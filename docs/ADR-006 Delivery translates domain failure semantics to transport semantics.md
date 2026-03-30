# ADR-006: Delivery översätter domänens felsemantik till transportsemantik

## Status
Accepterad — med kända avvikelser i implementation

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

## Kända avvikelser

Den nuvarande implementationen i `DomainMappingExtensions.HandleExceptions` avviker från beslutets principer på två sätt:

### 1. Global typ-till-status-mappning utan epistemisk motivering

Beslut §5 förbjuder globala mappningar utan fullständig epistemisk grund. Den aktuella koden gör precis detta:

```csharp
DomainInvariantViolationException => 409
DomainArgumentFormatException => 400
_ => 500
```

Varje mappning bör motiveras utifrån HTTP-ytans semantik, inte bara undantagstypen.

### 2. Ofullständig undantagshantering

Fem av sju `DomainException`-typer faller igenom till `_ => 500`:
- `DomainValidationException` — kastas av `Personnummer.Parse` och `HsaId.Tolka`, vanliga vid felaktig indata. Faller till 500 i stället för ett klientvänligt svar.
- `DomainAggregateNotFoundException` — kastas av `Throw.Region.Saknas` och `Throw.Person.Saknas`. Faller till 500 i stället för 404.
- `DomainInvalidStateTransitionException` — kastas av `Throw.Vårdval.RedanAvslutat`, `Throw.Person.IngetAktivtVårdvalAttStänga` m.fl. Faller till 500.
- `DomainConcurrencyConflictException` — faller till 500.
- `DomainIdempotencyViolationException` — faller till 500.

Detta innebär att klienter får generiska 500-svar för domänfel som borde ge meningsfull feedback.

### Rekommendation

Antingen motivera varje mappning per HTTP-yta (som beslutet föreskriver) eller medvetet tillåta en pragmatisk default-mappning för referensarkitekturen med tydlig dokumentation om varför varje undantagstyp → statuskod-par är semantiskt korrekt i detta sammanhang.

## Relaterade dokument
- `docs/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/ADR-003 Domain failure vocabulary.md`
- `docs/ADR-004 Semantic precision in exception factories.md`
- `docs/ADR-005 Verification of domain failure semantics.md`
- `docs/ADR-007 Dual failure channel.md`
- `docs/POLICY.md`
