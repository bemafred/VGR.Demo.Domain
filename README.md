# VGR Demo – Domain + Technical + EF (CQRS-light)

Detta paket visar arkitekturen:
- **Domain**: rika aggregat (Region, Person, Vardval), VO’s, invariants, Domain Events.
- **Technical**: `Outcome`, minimal **DQ (Domain Queries)**-kärna.
- **Infrastructure.EF**: EF Core-konfigurationer, Read/Write DbContexts.

## Struktur
```
src/
  VGR.Domain/
    SharedKernel/ (VO + exceptions + Throw)
    Region.cs, Person.cs, Vardval.cs
  VGR.Semantics.Abstractions/
  VGR.Semantics.Generator/
  VGR.Sementics.Queries/
  VGR.Sementics.Queries.Tests/
  VGR.Technical/
    Utfall.cs, Dq.cs
  VGR.Infrastructure.EF/
    Configs/ (PersonConfig, VardvalConfig, RegionConfig)
    ReadDbContext.cs, WriteDbContext.cs
```

## Principer
- Domän är **ren & synkron**: inga EF/async/ct; mutation via beteenden.
- Barnmängder: **privat List<T>** + **publik IReadOnlyList<T>**.
- CQRS-light: **ReadDbContext** (NoTracking), **WriteDbContext** (tracking).
- Pushdown först: läsningar som projektion/Any; skrivningar laddar minsta nödvändiga.

## Bygg
Kräver .NET 10 SDK.
```bash
dotnet build src/VGR.Infrastructure.EF/VGR.Infrastructure.EF.csproj -c Release
```

## DI-exempel
```csharp
services.AddDbContext<ReadDbContext>(o => o.UseSqlServer(cs)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
services.AddDbContext<WriteDbContext>(o => o.UseSqlServer(cs));
```
## Application + Web

Nu finns även:
- `VGR.Application` med `SkapaPersonInteractor` (pushdown + fabrik på Region).
- `VGR.Web` med en `PersonsController`, en enkel `Map`-klass för Outcome/Exceptions→HTTP, och `Program.cs` (InMemory DB för demo).

### Kör webben (demo)
```bash
dotnet run --project src/VGR.Web/VGR.Web.csproj
# POST http://localhost:5000/api/regioner/{regionId}/personer
```


### Nya exempel (Interactor + Controller)
- `VGR.Application/Vardval/SkapaVardvalInteractor.cs` – pushdown-överlappskontroll, filtrerad explicit load, `Person.SkapaVardval(...)`, save.
- `VGR.Web/Controllers/VardvalController.cs` – `POST /api/personer/{personId}/vardval` med DTO.


## Tester (xUnit + SQLite in-memory)

Projekt **VGR.Tests** använder SQLite in-memory (relations-likt) och kör end-to-end mot interaktorerna.

Kör tester:
```bash
dotnet test src/VGR.Tests/VGR.Tests.csproj -c Release
```

Tester som ingår:
- `CreatePerson_Then_CreateVardval_Succeeds` – E2E: Region → SkapaPerson → SkapaVårdval → verifiera persistens
- `CreateVardval_Overlapping_ReturnsFail` – pushdown-kollen stoppar överlapp


## Domän-enhetstester

Projekt **VGR.Domain.Tests** testar aggregat och VO helt utan EF/infrastruktur.

Kör:
```bash
dotnet test src/VGR.Domain.Tests/VGR.Domain.Tests.csproj -c Release
```


## Analyzers (Domain guardrails)

Projekt **VGR.Analyzers** innehåller Roslyn-regler som appliceras på `VGR.Domain`:
- `VGR001` – förhindrar `public set` på domän-egenskaper
- `VGR002` – förhindrar publika muterbara samlingar (ICollection/IList/List)

Se `ANALYZER_REGLER.md`. Severity konfigureras i `.editorconfig` (default = error).
