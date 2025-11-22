# VGR Arkitektur — POLICY

## Felhantering och kontrollflöde

Systemet använder två kompletterande mekanismer:

- **Throw**: används i domänen för att signalera regelbrott (invariants, argumentfel, concurrency-konflikter).
- **Utfall**: används i interaktorer och queries för att returnera lyckade eller misslyckade resultat utan att kasta.

### Policy

| ***Typ***   | ***Mönster***                                                                 | ***Hantering***                             |
|-------------|-------------------------------------------------------------------------------|---------------------------------------------|
| **Command** | Kastar `DomainException` via `Throw` eller `Utfall<T>` vid effektivitetsbehov | Controller fångar och mappar till HTTP-svar |
| **Query**   | Returnerar `Utfall<T>`                                                        | Controller returnerar resultat direkt       |

Detta möjliggör maximal prestanda och tydlighet: interaktorer kan välja den modell som passar bäst.

## CQRS-Light

Två separata `DbContext` används för läs och skriv:

- `ReadDbContext` → `AsNoTracking()`, används för queries/projektioner.
- `WriteDbContext` → används för kommandohantering och aggregerad persistens.

Detta ger “CQRS-light” utan ceremoni – enkelt, testbart och mycket snabbt.

## Domänens ansvar

- **Domänen kastar** när regler bryts.  
- **Interaktorn** fångar vid särskilda eller mappar till `Utfall<T>`.  
- **Kontroller** ansvarar för mappning av request/ och kan fånga och mappa `Throw`

## Strategi

Vi använder **SQLite in-memory** för alla automatiserade tester. Denna val
unifieras genom `VGR.Technical.Testing.SqliteHarness`.

## Testning & SqliteHarness

#### Varför in-memory?

- **Verklig relationell semantik** – inte mocks, real SQL execution
- **Snabb** – in-memory ⟹ ingen disk-IO
- **Deterministisk** – repeterbar test-miljö
- **Korrelationsvalidering** – verifiera domän ≡ SQL
- **CQRS-light transparent** – Read/Write contexts are testable

### Tre testlager

| Lager | Projekt | Fokus | Harness |
|-------|---------|-------|---------|
| **Domain** | `VGR.Domain.Tests` | Aggregat, VO, invariants | Nej – pure C# |
| **Semantic** | `VGR.Semantics.Linq.CorrelationTests` | Domänmetoder → SQL-korrekthet | **Ja** – SqliteHarness |
| **E2E/Integration** | `VGR.Tests` | Interaktorer, web, end-to-end | **Ja** – SqliteHarness |
