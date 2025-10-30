—

## ⚙️ **docs/POLICY.md**
```markdown
# VGR Arkitektur — POLICY

## Felhantering och kontrollflöde

Systemet använder två kompletterande mekanismer:

- **Throw**: används i domänen för att signalera regelbrott (invariants, argumentfel, concurrency-konflikter).
- **Outcome**: används i interaktorer och queries för att returnera lyckade eller misslyckade resultat utan att kasta.

### Policy

| Typ | Mönster | Hantering |
|——|————|————|
| **Command** | Kastar `DomainException` via `Throw` | Controller fångar och mappar till HTTP-svar |
| **Query** | Returnerar `Outcome<T>` | Controller returnerar resultat direkt |

Detta möjliggör maximal prestanda och tydlighet: interaktorer kan välja den modell som passar bäst.

## CQRS-Light

Två separata `DbContext` används för läs och skriv:

- `ReadDbContext` → `AsNoTracking()`, används för queries/projektioner.
- `WriteDbContext` → används för kommandohantering och aggregerad persistens.

Detta ger “CQRS-light” utan ceremoni – enkelt, testbart och mycket snabbt.

## Domänens ansvar

- **Domänen kastar** när regler bryts.  
- **Interaktorn fångar** och mappar till Outcome eller HTTP-respons.  
- **Kontroller** ansvarar endast för mappning av request/