# ADR-014: Compile-time separation mellan delivery och infrastruktur

## Status
Föreslagen

## Kontext

VGR001 och VGR002 skyddar domänlagret via Roslyn-analyzers — publika setters och muterbara collections ger kompileringsfel. Dessa regler gäller universellt inom `VGR.Domain`-namnrymden och har noll undantag.

Delivery-lagret (controllers) har en annan typ av regel: **controllers ska delegera, inte orkestrera**. Det innebär att en controller aldrig ska referera `DbContext` direkt — all dataåtkomst sker via interactors i `VGR.Application`. Detta beslut är dokumenterat i ADR-013 §5 och motiverat av arkitekturell symmetri (CQRS-light).

Idag finns inget som hindrar en utvecklare från att injicera `WriteDbContext` i en controller och köra queries direkt. Arkitekturtester (NetArchTest-stil) fångar detta — men först vid testkörning. En Roslyn-analyzer fångar det *medan utvecklaren skriver koden*. Tidig återkoppling sparar mest tid.

### Avgränsning

Denna ADR adresserar en enda regel. Det är medvetet — varje analyzer-regel ska motiveras individuellt. En regelmaskin som växer okontrollerat är ett anti-mönster.

## Beslut

### VGR003: Ingen DbContext-åtkomst i controllers

En ny diagnostikregel i `VGR.Analyzers`:

- **ID**: `VGR003`
- **Severity**: Error
- **Scope**: Klasser i namnrymder som matchar `*.Controllers` och som ärver `ControllerBase`
- **Trigger**: Fält, property, parameter eller lokal variabel vars typ ärver `Microsoft.EntityFrameworkCore.DbContext`
- **Meddelande**: `"Controller '{0}' får inte referera DbContext direkt. Delegera till en interactor i VGR.Application."`

### Implementation

Analyzern utökar befintlig `DomainGuardAnalyzer` eller skapas som separat `DeliveryGuardAnalyzer` i samma projekt. Registrerar en `RegisterSymbolAction` på `SymbolKind.NamedType` för klasser som ärver `ControllerBase`, sedan inspekterar konstruktorparametrar och fält.

## Konsekvenser

### Positiva
- **Tidig återkoppling**: röd understrykning i IDE:n medan koden skrivs — inte vid testpass
- **Konsistens**: tre lager skyddade av analyzers (domän: VGR001/002, delivery: VGR003)
- **Pedagogiskt**: felmeddelandet pekar mot rätt mönster ("delegera till interactor")

### Negativa
- **En till analyzer att underhålla** — men regeln är stabil (controllers ska aldrig prata med DB)
- **Kräver att VGR.Analyzers refereras av VGR.Web** — redan fallet idag

### Neutrala
- `VGR.Technical.Web` (system-UI) undantas — `/data`-endpoints använder reflection-driven DbContext-access medvetet
