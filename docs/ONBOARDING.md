# ONBOARDING.md

## VGR --- E-Clean & Semantic Architecture

Välkommen in i VGR:s Semantic Architecture.\
Det här dokumentet hjälper dig att förstå hur lösningen hänger ihop och
var du som utvecklare normalt arbetar.

Fokusera gärna på följande fyra saker:

1.  **Domänen är sanningen.**\
2.  **Domain.Queries ger projektioner och vyer.**
3.  **Semantics.Linq ger LINQ-förmåga för domänbegrepp.**
4.  **Expansions i VGR.Infrastructure.EF talar om hur domänmetoder översätts till EF.**

När du ser flödet mellan de här delarna blir resten naturligt.

------------------------------------------------------------------------

## 1. Domänen --- vad som är sant

I `VGR.Domain` definieras vår verkliga verksamhetsmodell:

-   Aggregat\
-   Värdeobjekt (t.ex. `Tidsrymd`)\
-   Invariants\
-   Domänspråk (metoder som `Innehåller`, `Överlappar`, `ÄrAktivt`)\
-   Semantiska attribut (t.ex. `[SemanticQuery]`)

All affärslogik bor här.\
Om ett begrepp saknas i domänen är det **domänen** som ska utökas --
inte queries, expansions eller EF.

------------------------------------------------------------------------

## 2. Semantics.Linq --- möjligheten att använda LINQ för domänbegrepp

`VGR.Semantics.Linq` är ett **expression-rewriter-lager** som gör att
du kan skriva LINQ med domänmetoder och ändå få korrekt SQL.

Exempel i domänen:

``` csharp
public sealed record Tidsrymd(DateTimeOffset Start, DateTimeOffset? Slut)
{
    public bool Innehåller(DateTimeOffset t) => /* domänlogik */;
    public bool Överlappar(Tidsrymd annan)   => /* domänlogik */;
    public bool ÄrTillsvidare                => Slut is null;
}
```

Exempel i en query:

``` csharp
var aktiva = db.Vårdval
    .WithSemantics()
    .Where(v => v.Period.Innehåller(clock.UtcNow));
```

Detta ser naturligt ut i domänspråket -- men EF förstår inte
`Innehåller` av sig självt.

------------------------------------------------------------------------

## 3. Expansions i `VGR.Infrastructure.EF` --- hur domänmetoder översätts till EF

I `VGR.Infrastructure.EF/Expansions` definieras expansionsmetoder för
domänmetoder. De är markerade med `[ExpansionFor]` och returnerar
EF-vänliga `Expression<Func<...>>`.

Exempel -- `Tidsrymd`:

``` csharp
[ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.Innehåller))]
public static Expression<Func<Tidsrymd, DateTimeOffset, bool>> Innehåller_Expansion()
    => (r, t) => r.Start <= t && (r.Slut == null || t < r.Slut);
```

Exempel -- `Vårdval`:

``` csharp
[ExpansionFor(typeof(Vårdval), nameof(Vårdval.ÄrAktivt))]
public static Expression<Func<Vårdval, bool>> ÄrAktivt_Expansion()
    => v => v.Period.ÄrTillsvidare;
```

Dessa expansions används av `SemanticRegistry` för att översätta
domänmetoder till rena LINQ-uttryck som EF kan förstå och översätta till
SQL.

De flesta utvecklare behöver aldrig röra expansionsfilerna --- de är en
del av den tekniska infrastrukturen.

------------------------------------------------------------------------

## 4. Hur Semantics.Linq + Expansions fungerar i en query

När du skriver:

``` csharp
db.Vårdval
  .WithSemantics()
  .Where(v => v.Period.Innehåller(clock.UtcNow));
```

sker följande:

1.  `WithSemantics()` byter ut query-provider till
    `SemanticQueryProvider`.
2.  Provider skickar uttrycksträdet till
    `SemanticRegistry.Rewrite(...)`.
3.  Rewrite ersätter domänmetoden med expansionens EF-vänliga uttryck.
4.  EF får ett rent uttryck (`<=`, `<`, `&&`) och genererar SQL.

Resultat:

-   du skriver i domänspråk
-   domänen behåller sin sanning
-   EF får standardiserade uttryck

Semantics.Linq implementerar LINQ-förmågan --- expansions visar
vägen.

------------------------------------------------------------------------

## 5. Domain.Queries --- projektioner och vyer

`Domain.Queries` används för:

-   DTO:er
-   read-models
-   projektioner
-   vyer för API/UI

Det är inte logik och inte semantik --- bara output.

En projection uttrycker:

> "Så här vill vi presentera domänens sanningsmodell i detta
> sammanhang."

------------------------------------------------------------------------

## 6. Application & Infrastructure -- var du arbetar

-   I `Application` skapar du interaktorer som:
    -   använder domänen
    -   uttrycker frågor via Semantics.Linq
    -   formar utdata via Domain.Queries
-   I `Infrastructure.EF` finns:
    -   DbContext
    -   EF-konfiguration
    -   expansions (avancerad del, sällan ändrad)

I normal vardag:

-   nytt begrepp → domänen
-   ny fråga i LINQ → expansions + Semantics.Linq
-   ny projektion → Domain.Queries
-   nytt use case → Application

------------------------------------------------------------------------

## 7. Vanliga misstag

❌ Skriva EF-logik i interaktorer
✅ Använd Semantics.Linq

❌ Blanda semantik med affärslogik
✅ Domänen först

❌ Skapa projections i EF-lagret
✅ Domain.Queries är rätt plats

❌ Lägga EF-specifik logik i domänen
✅ Håll det i expansions + Infrastructure

------------------------------------------------------------------------

## 8. Du behöver inte förstå allt dag ett

Du behöver inte:

-   kunna expression visitors
-   skriva expansions
-   hantera rewriters

Det viktiga är:

1.  Domänen uttrycker sanning
2.  Semantiken gör domänen querybar
3.  Expansions översätter domänens språk till EF
4.  Projektioner format anpassat för konsumenter

Resten kommer med tiden.

------------------------------------------------------------------------

## 9. Testning — SqliteHarness och in-memory DB

VGR använder **SQLite in-memory** för nästan all testning. Denna strategi
unifieras via `VGR.Technical.Testing.SqliteHarness`.

### Varför SQLite in-memory?

- **Verklig relationell semantik** – inte bara mock-objekt
- **SQL-validering** – säkerställ att domänmetoder översätts korrekt till SQL
- **Snabb** – i-minne ⟹ ingen disk-IO
- **Deterministisk** – repeterbar testning

-------------------------------------------------------------------------

## 10. Technical Domain — tekniska byggblock och orthogonalitet

`VGR.Technical` och `VGR.Technical.Testing` m.m. utgör **Technical Domain**.

Det är en speciell domän för tekniska begrepp som är ortogonala till affärsdomänen:

- **`Utfall<T>`** – designbeslut för resultat-hantering (icke-exceptionella misslyckanden)
- **`IClock`** – tid-abstraktion (testbarhet, deterministism)
- **`SqliteHarness`** – unified testinfrastruktur

### Varför separat domän?

Dessa begrepp behövs från **flera håll** — Application, Infrastructure, Tests — men de är inte del av affärsspråket.

En utvecklare behöver inte tänka på `IClock` när hen modellerar Vårdval, men interaktorer använder det för att injicera tid.

### Vad gör jag här?

Normalt: ingenting. Technical Domain är stabil.

Om du behöver ett nytt tekniskt begrepp:
1. Fråga: "Är detta ortogonalt?" (kan det användas överallt utan affärskontext?)
2. Om ja: lägg det i `VGR.Technical`
3. Om nej: det hör hemma i någon annan domän

------------------------------------------------------------------------


Välkommen --- arkitekturen är byggd för att bära dig.
