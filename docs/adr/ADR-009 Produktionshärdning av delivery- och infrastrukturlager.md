# ADR-009: Produktionshärdning av delivery- och infrastrukturlager

## Status
Accepterad

## Kontext

Referensarkitekturen ska demonstrera hur en produktionsklar lösning ser ut. ADR-000 fastslår att E-Clean och Semantic Architecture inte bara handlar om domänmodellering utan om helheten: epistemisk klarhet, förklarbarhet och evolvbarhet.

En granskning av lösningen visar att domänlagret (ADR-003–008) är väl genomarbetat, men delivery- och infrastrukturlagren har systematiska luckor som inte lever upp till referensarkitekturens ambitionsnivå:

1. **Infrastrukturfel sväljs** — `OperationCanceledException`, `DbUpdateConcurrencyException` och `DbUpdateException` ger alla generiska 500-svar.
2. **Ingen samtidighetskontroll** — EF-konfigurationer saknar concurrency tokens; parallella uppdateringar ger last-write-wins utan varning.
3. **Trådsäkerhet** — `SemanticRegistry` använder statisk `Dictionary` utan synkronisering.
4. **API-kontrakt** — `ProblemDetails.Type` saknas (RFC 9457), inga `ProducesResponseType`-attribut, inkonsekvent felformat mellan domänfel och `Utfall.Fail`.
5. **Noll observerbarhet** — ingen loggning vid felgränsen; stacktraces och inner exceptions försvinner.
6. **Ingen gränsvalidering** — DTO:er saknar valideringsattribut; `Guid.Empty` passerar rakt igenom till domänen.
7. **Utfall saknar maskinläsbar kod** — klienter kan inte programmatiskt agera på affärsfel.

Dessa luckor underminerar referensarkitekturens trovärdighet som demonstration av produktionsstandard.

## Beslut

### 1. Delivery ska hantera infrastrukturella undantag explicit

`HandleExceptions` ska utökas med:

| Undantag | HTTP | Motivering |
|----------|------|------------|
| `OperationCanceledException` | — (inget svar) | Klienten har redan gått; att skicka svar är meningslöst |
| `DbUpdateConcurrencyException` | 409 | Optimistisk samtidighetskonflikt — klienten kan försöka igen |
| `DbUpdateException` | 422 | Databaskonstraint bruten — klientens data är ogiltigt i relation till befintligt tillstånd |

### 2. Aggregat ska ha concurrency tokens

Varje aggregatrot (`Region`, `Person`, `Vårdval`) ska ha en `RowVersion`-property som EF mappar med `IsRowVersion()`. Detta möjliggör optimistisk samtidighetskontroll och ger `DbUpdateConcurrencyException` vid konflikter.

### 3. SemanticRegistry ska vara trådsäker

Den statiska `Dictionary<MethodInfo, LambdaExpression>` ska ersättas med `ConcurrentDictionary` eller skyddas med lås vid initiering.

### 4. ProblemDetails ska följa RFC 9457

Alla felsvar ska ha:
- `Type` — URI som identifierar problemtypen (t.ex. `urn:vgr:domain:invariant-violation`)
- `Title` — kort, mänskligt läsbar rubrik
- `Status` — HTTP-statuskod
- `Detail` — domänens felmeddelande
- `Extensions["code"]` — domänens stabil felkod

`Utfall.Fail`-svar ska följa samma format med `Type` satt till `urn:vgr:application:business-failure`.

### 5. Loggning vid felgränsen

`Map` ska injicera `ILogger` och logga:
- Domänfel på Warning-nivå (förväntat men notebart)
- Infrastrukturfel på Error-nivå (oväntat, kräver uppmärksamhet)
- Avbrutna requests på Information-nivå

### 6. Utfall ska kunna bära maskinläsbar kod

`Utfall<T>.Fail` utökas med valfri `code`-parameter:

```csharp
public static Utfall<T> Fail(string error, string? code = null)
```

Befintliga anrop behåller sitt beteende. Delivery bär `code` i `Extensions["code"]` om den finns.

### 7. API-ytan ska vara deklarerad

Controllers ska ha `[ProducesResponseType]`-attribut för alla förväntade statuskoder. Detta möjliggör korrekt OpenAPI-generering.

### 8. Indata ska valideras vid systemgränsen

DTO:er ska ha valideringsattribut (`[Required]`, `[StringLength]`, etc.). Route-parametrar av typen `Guid` ska avvisas om de är `Guid.Empty`.

## Konsekvenser

### Fördelar
- Referensarkitekturen demonstrerar produktionsklar felhantering i alla lager
- Klienter får konsekvent, maskinläsbar felinformation
- Operatörer kan diagnostisera fel
- Samtidighetsproblem upptäcks och kommuniceras istället för att korrumpera data
- API-dokumentation genereras automatiskt och korrekt

### Nackdelar
- Fler konfigurationsdetaljer i EF-configs och controllers
- `Utfall`-signaturen ändras (bakåtkompatibelt med valfri parameter)
- RowVersion kräver databasmigration vid produktionssättning

## Avgränsning

Följande hör till deployment och drift, inte referensarkitekturen (se ADR-000 §190–197):
- Autentisering och auktorisering
- Rate limiting och circuit breakers
- Soft deletes och arkivering
- Content negotiation

## Relaterade dokument
- `docs/adr/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/adr/ADR-006 Delivery translates domain failure semantics to transport semantics.md`
- `docs/adr/ADR-007 Dual failure channel.md`
