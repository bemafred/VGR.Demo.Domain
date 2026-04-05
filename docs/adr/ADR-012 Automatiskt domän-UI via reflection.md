# ADR-012: Automatiskt domän-UI via reflection

## Status
Genomförd

## Kontext

ADR-000 fastslår att domänens språk ska vara första klassens medborgare genom hela stacken.
ADR-003–008 etablerar en domän som skyddar sig själv: `Throw`-hierarki, analyzers (`VGR001`/`VGR002`),
inga publika set, barnmängder som `IReadOnlyList<T>`. ADR-009 härdar delivery-lagret med centraliserad
`HandleExceptions`-mappning av alla domänfel till RFC 9457 `ProblemDetails`.

System-UI:t (`VGR.Technical.Web`) exponerar idag tre statiska, read-only sidor:
- `/` — indexsida
- `/domain` — reflection-driven domänstrukturvy via `SemanticRegistry.GetModel()`
- `/api` — reflection-driven endpoint-vy via `EndpointDataSource`

Systemet kan *visa sig självt* men inte *interagera med sig självt*.

Samtidigt har arkitekturen redan alla mekaniska förutsättningar:

1. **SemanticRegistry/DomainModelBuilder** — typkatalog med klassificering
   (Aggregate, Entity, Identity, ValueObject), metoder (inkl. `IsStatic`), properties.
2. **ReadDbContext** (NoTracking) — `DbSet<Region>`, `DbSet<Person>`, `DbSet<Vårdval>`.
3. **WriteDbContext** — samma DbSets med change tracking.
4. **HandleExceptions** — fullständig mappning av domänundantag till HTTP-statuskoder.
5. **Semantic Core** — `WithSemantics()` översätter domänmetoder till EF-kompatibel LINQ.

## Problem

För utveckling, felsökning och underhåll behövs möjligheten att:
- Lista aggregatinstanser och navigera relationer (Region → Personer → Vårdval)
- Anropa domänens publika fabriksmetoder och mutatorer direkt
- Se resultatet — inklusive domänfel — utan att skriva controllers, DTOs eller endpoints

Idag kräver varje ny use-case en handskriven controller, en DTO och en interaktor.
Det är korrekt för produktions-delivery (nivå 1: deklarativa, tunna controllers).
Men för ett utvecklar-/admin-verktyg är ceremonin onödig — domänens publika yta *är* API:t.

## Beslut

### 1. `/data`-routes registreras i `MapDomainEndpoints`

Nya routes läggs till i samma `MapDomainEndpoints`-metod i `VGR.Technical.Web`:

```
GET  /data                        → lista aggregattyper
GET  /data/{type}                 → lista instanser
GET  /data/{type}/{id}            → visa instans med relationer
GET  /data/{type}/{id}/{relation} → navigera relation

POST /data/{type}/{method}        → statisk fabriksmetod (skapa aggregat)
POST /data/{type}/{id}/{method}   → instansmetod (mutera befintligt aggregat)
```

`app.UseDomain()` i `Program.cs` förblir den enda registreringsraden — `/data` ingår
automatiskt, precis som `/domain` och `/api`.

### 2. Listning via ReadDbContext, mutation via WriteDbContext

DbContexts resolvas från `IServiceProvider` vid request-tid — ingen kompileringstidsberoende
från `VGR.Technical.Web` till `VGR.Infrastructure.EF`. Samma mönster som ASP.NET Core:s
inbyggda middleware.

DbSet-lookup sker via `DbContext.Model.GetEntityTypes()` och matchning mot typnamn från URL:en.

### 3. Reflection-invocation av domänens publika yta

Mutation sker genom att:
1. Identifiera metoden via `DomainModel.Methods` (redan extraherad av `DomainModelBuilder`)
2. Deserialisera parametrar från JSON-body
3. Anropa via reflection: `MethodInfo.Invoke()`
4. Persistera via `WriteDbContext.SaveChangesAsync()`

Kedjan: **UI → reflection → domänmetod → invarianter skyddar → WriteDbContext → persistens**.

Interaktorerna kringgås medvetet. De finns för specifik orchestration (pushdown, dubblettcheck).
Utvecklar-UI:t vill komma åt domänens primitiva operationer direkt. Invarianterna i domänen
skyddar oavsett — det är arkitekturens grundantagande.

### 4. Domänens invarianter som säkerhetsnät

Arkitekturen garanterar att ogiltigt state inte kan uppstå via den publika ytan:
- `Throw`-hierarkin kastar vid regelbrott
- `HandleExceptions` mappar till RFC 9457-svar
- Analyzers förhindrar publika set och muterbara samlingar
- Barnmängder exponeras som `IReadOnlyList<T>`

Reflection-baserad invocation genom den publika ytan ger samma garantier som handskriven kod.

### 5. HTML + vanilla JavaScript

Formulär genereras server-side från metodsignaturer. JavaScript (`fetch()`) skickar
anrop och visar resultat inline — statuskod, response-body, domänfel.

Inga ramverk, inga byggsteg, inga npm-beroenden. Progressiv förbättring: sidorna
fungerar som read-only utan JavaScript.

### 6. Explicit avgränsat som utvecklarverktyg

UI:t markeras tydligt som utvecklar-/adminverktyg. Inte riktat till slutanvändare.
Säkerhet och autentisering är out-of-scope för referensarkitekturen.

## Konsekvenser

### Fördelar

- **Emergent funktionalitet** — features som inte designades explicit uppstår ur arkitekturens
  befintliga garantier. Det är det starkaste beviset för att abstraktionerna bär.
- **Noll ceremoni** — nya aggregat/metoder exponeras automatiskt. Ingen handskriven controller
  eller DTO behövs.
- **Domän-REPL** — utvecklare kan utforska domänens beteende interaktivt, testa edge cases,
  manuellt korrigera data.
- **Arkitekturbevis** — om domänen kan exponeras rakt av via reflection och ändå skydda sig
  själv, har vi visat att domänsuveräniteten är reell, inte ceremoniell.

### Nackdelar

- **Reflection-invocation kräver vaksamhet** — vilka metoder som exponeras måste vara kontrollerat.
  Heuristik: publika metoder på aggregat/entiteter i de assemblies som registrerats via `UseDomain()`.
- **Kringgår interaktorernas orchestration** — medvetet val, men innebär att pushdown-optimeringar
  och orchestration-logik inte tillämpas. Utvecklaren ansvarar för att förstå skillnaden.
- **Parameterkonvertering** — fabriksmetoder som tar värdeobjekt (inte primitiver) kräver
  rekursiv deserialisering. Kan introduceras stegvis.

### Invariant

Produktions-delivery (deklarativa controllers + interaktorer) påverkas inte. `/data`-routes
existerar parallellt och ersätter inget.

## Avgränsning

- Säkerhet och autentisering behandlas inte — referensarkitekturen har inget auth-lager.
- Observation (realtidsström av requests, domain events, SQL) behandlas i separat ADR.
- Semantiska filter (`?filter=ÄrAktivt`) via `WithSemantics()` är ett naturligt nästa steg
  men ingår inte i denna ADR.

## Implementationsstatus

- [x] ADR dokumenterad
- [x] Steg 1: `/data` med listning av aggregat och instanser (ReadDbContext)
- [x] Steg 2: Navigation av relationer (`/data/{type}/{id}/{relation}`)
- [x] Steg 3: POST-routes för fabriksmetod-invocation (WriteDbContext)
- [x] Steg 4: HTML-formulär med vanilla JS

## Relaterade dokument

- ADR-000 — E-Clean & Semantic Architecture
- ADR-003 — Domain failure vocabulary
- ADR-009 — Produktionshärdning av delivery- och infrastrukturlager
- ADR-011 — Compile-time verifiering av semantiska expansioner
