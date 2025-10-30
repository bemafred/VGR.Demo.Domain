# VGR Arkitektur — PLACERING

Detta dokument beskriver hur de olika projekten i lösningen är organiserade och vilken roll de spelar i helheten.

## Struktur

| Projekt | Syfte |
|----------|-------|
| **VGR.Domain** | Verksamhetsdomän: aggregat, värdeobjekt, invariants, `Throw` |
| **VGR.Technical** | Teknisk domän: `Outcome`, `Map`, intern infrastruktur för interaktorer |
| **VGR.Application** | Interaktorer (kommandon och queries) |
| **VGR.Infrastructure.EF** | Entity Framework-konfiguration och `DbContext` |
| **VGR.Analyzers** | Roslyn-analyzers som upprätthåller domänregler |
| **VGR.Tests** | End-to-end-tester med SQLite in-memory |
| **VGR.Domain.Tests** | Enhetstester av domänen |

## Principer

- Domänen är suverän – inga beroenden till EF, applikation eller infrastruktur.  
- - **Felhantering sker med `Throw` eller `Outcome`.**  
  - `Throw` används för invariants och fel som *ska* bryta exekveringen – både i domän och applikationslager.  
  - `Outcome` kan användas när det finns skäl att undvika undantag, t.ex. av prestandaskäl eller för att uttrycka icke-exceptionella misslyckanden i interaktorer.  
- Technical innehåller generella byggblock som `Map` och `Outcome`.  
- Application implementerar interaktorer som anropar domänen och returnerar Outcome.  
- Infrastructure.EF ansvarar för mappning, persistens och pushdown-konfiguration.  
- Analyzers säkerställer att inga regler bryts i domänen (t.ex. public set, List).  
- Tests är fullstack-integration med SQLite; Domain.Tests isolerar domänlogik.  
- CQRS används för att separera kommandon (skrivoperationer) från queries (läsoperationer).

