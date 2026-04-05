# ADR-013: RegionKod som värdeobjekt, masterdata-seed och query-interactors

## Status
Föreslagen

## Kontext

### Problem 1: Kod är en naked string

Region-aggregatet har idag en `Kod`-egenskap som är en naked `string`. Detta bryter med det värdeobjektsmönster som `Personnummer` och `HsaId` redan etablerar — båda har `Tolka()`/`FörsökTolka()`, validering och normalisering.

Regioner saknar dessutom `Namn` ("Västra Götalandsregionen") och `Förkortning` ("VGR") — egenskaper som behövs för meningsfull presentation.

En djupare fråga: regioner är **masterdata**. De uppfinns inte av systemet — de finns redan. `Region.Skapa("14")` skapar inte en region i verkligheten, den registrerar en känd entitet. Det skiljer sig fundamentalt från `Person` eller `Vårdval` som skapas av domänoperationer.

### Problem 2: API:et saknar läsoperationer

API:et har idag bara command-endpoints (`POST`). Det finns inga `GET`-endpoints för att lista eller hämta regioner, personer eller vårdval. En referensarkitektur som demonstrerar CQRS-light behöver visa båda sidorna.

### Problem 3: Arkitekturell symmetri

Command-endpoints använder interactors i `VGR.Application` — controllern delegerar, interaktorn orkestrerar. Om GET-endpoints implementeras direkt i controllers skapas ett prejudikat som bryter detta mönster. Läsningar kan behöva komposition, projektion och semantiska queries (det som `VGR.Domain.Queries` är tänkt för). Att query-interactors idag skulle vara triviala motiverar inte att hoppa över dem — arkitekturen tar höjd för att de växer.

### Avgränsning: varför inte Namn/Förkortning som värdeobjekt?

`Kod` identifierar — den är en nyckel i ett nationellt system (SCB:s regionkoder). Den har format, regler och semantik. `Namn` och `Förkortning` är beskrivande metadata utan domänregler. De förtjänar fabriksvalidering (icke-tomma), men inte värdeobjektsstatus.

## Beslut

### §1 — RegionKod som värdeobjekt

En ny `readonly record struct RegionKod` i `SharedKernel` med:
- `string Value` (kanonisk tvåsiffrig form)
- `Tolka(string)` / `FörsökTolka(string, out RegionKod)` enligt etablerat mönster
- Validering: exakt två siffror (`00`–`99`)
- `implicit operator string` för ergonomisk användning

### §2 — Region får Namn och Förkortning

`Region.Skapa(RegionKod kod, string namn, string förkortning)` ersätter `Region.Skapa(string kod)`. Fabriken validerar att namn och förkortning inte är tomma.

### §3 — Masterdata via domänfabrik

Regioner seedas vid applikationsstart genom `Region.Skapa()` — inte via EF `HasData()`. Seedning genom domänfabriken garanterar att alla invarianter gäller även för masterdata. Idempotent: hoppar över om regioner redan finns.

### §4 — EF-konversion för RegionKod

`RegionConfig` uppdateras med `HasConversion` för `RegionKod` samt kolumndefinitioner för `Namn` och `Förkortning`. `Kod` får `HasMaxLength(2)` och unikt index.

### §5 — Query-interactors för GET-endpoints

GET-endpoints implementeras med samma arkitekturmönster som commands:

- **Query-interactors** i `VGR.Application` (t.ex. `HämtaRegionerInteractor`, `HämtaPersonInteractor`)
- Interaktorn använder `ReadDbContext` (NoTracking) och returnerar `Utfall<T>`
- Controllern delegerar — aldrig direkt databasåtkomst
- Projektioner och kompositioner sker i interaktorn, inte i controllern

Initiala GET-endpoints:

| Endpoint | Interactor |
|----------|-----------|
| `GET api/regioner` | `HämtaRegionerInteractor` |
| `GET api/regioner/{id}` | `HämtaRegionInteractor` |
| `GET api/regioner/{id}/personer` | `HämtaPersonerInteractor` |
| `GET api/personer/{id}` | `HämtaPersonInteractor` |

Controllern ansvarar för HTTP-semantik (statuskoder, content negotiation). Interaktorn ansvarar för dataåtkomst och eventuell orkestrering.

## Konsekvenser

### Positiva
- **Konsistens**: alla identifierande värden i domänen är nu värdeobjekt
- **Demovärde**: "vi la till tre properties och systemet visar dem automatiskt" — ADR-012:s reflection-design bevisas
- **Validering vid systemgräns**: `ParameterConverter` plockar upp `RegionKod.Tolka()` automatiskt
- **Arkitekturell symmetri**: reads och writes följer samma mönster — controller → interactor → context
- **Komplett demo-API**: CQRS-light demonstreras med både läs- och skrivsidan

### Negativa
- **~20 testställen** behöver uppdateras från `Region.Skapa("14")` till `Region.Skapa(RegionKod.Tolka("14"), "Västra Götalandsregionen", "VGR")`
- **PostgreSQL-schema**: nya kolumner kräver `EnsureCreated` eller manuell ALTER TABLE
- **Fler filer**: query-interactors är initialt tunna — men de finns på rätt plats för att växa

### Neutrala
- UI-lagren (`/data`, `/api`, `/domain`) kräver inga ändringar — reflection hanterar allt
