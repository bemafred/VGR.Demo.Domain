# ADR-008: Värdeobjektskontrakt, semantiska tolknings-APIer och odefinierade domänoperationer

## Status
Genomförd

## Kontext

ADR-003 fastslår att `Throw` är domänens kanoniska felvokabulär och att även värdeobjekt omfattas av denna princip.  
ADR-004 kräver semantisk precision i namn och fabriker.  
ADR-005 kräver att felsemantiken är verifierbar.  
ADR-006 visar att domänfel som inte uttrycks via `DomainException` riskerar att falla igenom som generiska 500-svar i delivery.  
ADR-002, `KODERGONOMI.md` och den övergripande arkitekturkanon slår fast att namn ska bära mening, inte primärt följa tekniska standardkonventioner.

Den nuvarande implementationen innehåller två relaterade brister i publika värdeobjekts-API:er:

1. Råa CLR/BCL-undantag förekommer i publika domän-API:er, främst i `Tidsrymd`, `Datumintervall` och `Throw.Användare.EjAuktoriserad`.
2. Namngivningen är semantiskt inkonsekvent:
   - `HsaId` använder `Tolka` / `FörsökTolka`
   - `Personnummer` använder `Parse` / `TryParse`

Detta skapar semantisk splittring:
- domänen talar flera felspråk
- värdeobjekt behandlas olika utan semantisk grund
- tekniska .NET-termer läcker in i en i övrigt svensk domän
- delivery kan inte konsekvent särskilja domänfel från tekniska fel
- referensarkitekturen bryter mot sina egna beslut

## Problem

Vi behöver en tydlig gräns mellan:
- rå extern representation som ska tolkas till ett domänvärde
- semantiskt ogiltiga domänvärden
- operationer som saknar mening för det aktuella värdet
- rent tekniska programmeringsfel

Vi behöver också besluta om publika värdeobjekts-API:er ska använda tekniska standardtermer (`Parse`, `TryParse`) eller domänspråkliga termer (`Tolka`, `FörsökTolka`).

Utan denna gräns blir både exceptionval och metodnamn godtyckliga.

## Beslut

### 1. Inga råa CLR/BCL-undantag i publika domän-API:er för domänsignifikanta fel

Publika metoder, properties och fabriker i `VGR.Domain` och `VGR.Domain.SharedKernel` får inte kasta råa `InvalidOperationException`, `ArgumentException`, `ArgumentOutOfRangeException`, `UnauthorizedAccessException` eller motsvarande när felet bär domänmening.

### 2. Värdeobjektens publika API ska uttryckas i svensk domänvokabulär

I den svenska domänen ska publika värdeobjekts-API:er använda svenska semantiska verb.

Därför gäller:
- `Tolka` används för kastande tolkning från extern representation till domänvärde
- `FörsökTolka` används för icke-kastande försök till tolkning

`Parse` och `TryParse` ska inte användas som publika domän-API:er i denna lösning när svensk semantisk benämning finns och är tydligare.

### 3. `Tolka` är semantiskt bättre än `Parse` i denna domän

`Tolka` används inte bara av språkliga skäl utan därför att operationen i flera fall gör mer än ren syntaktisk parsing.

Exempel:
- `Personnummer` tolkar separatorer, sekeltillhörighet, datumgiltighet och normaliserar till kanonisk representation
- `HsaId` tolkar och normaliserar extern representation till domänens kanoniska form

Detta är semantiskt närmare “tolkning” än ren parsing.

### 4. Felsemantik i värdeobjekt delas in i tre explicita kategorier

- `DomainArgumentFormatException`  
  Används när en rå extern representation inte kan tolkas till ett domänvärde.  
  Exempel: `Personnummer.Tolka(string)`, `HsaId.Tolka(string)`.

- `DomainValidationException`  
  Används när indata redan är typad men bryter mot en domänregel för giltighet.  
  Exempel: `slut < start`, `steg <= 0`.

- `DomainUndefinedOperationException`  
  Används för operationer som saknar semantisk mening för det aktuella värdet eller tillståndet.  
  Exempel: varaktighet för ett tillsvidare-intervall, enumerering av ett öppet intervall, omfång för tom sekvens.

### 5. Existerande aggregatkategorier behålls oförändrade

- `DomainInvariantViolationException` används fortsatt för brutna affärsinvarianter
- `DomainInvalidStateTransitionException` används fortsatt för otillåtna tillståndsövergångar

Dessa typer ska inte användas för att absorbera värdeobjektsfel som egentligen handlar om ogiltig konstruktion eller odefinierad operation.

### 6. `Throw` ska vara begreppsspecifik, inte generell

Nya fabriker ska placeras under det domänbegrepp som bär semantiken:
- `Throw.Personnummer`
- `Throw.HsaId`
- `Throw.Tidsrymd`
- `Throw.Datumintervall`

Det är förbjudet att införa generella samlingsplatser som `Throw.Common`, `Throw.ValueObject` eller motsvarande.

### 7. Fabriksnamn ska uttrycka exakt regel eller exakt odefinierad operation

Fabriker ska:
- använda `nameof` för stabil kod
- uttrycka en kanonisk semantisk benämning
- undvika tekniska namn som `InvalidOperation`, `BadArgument`, `NotAllowed`
- dokumenteras och verifieras som förstaklassig domänyta

### 8. Auktorisering är inte en generisk domänfabrik

`Throw.Användare.EjAuktoriserad` ska inte kvarstå i sin nuvarande form. En generisk auth-fabrik i domänen är semantiskt oklar.

- Om auktorisering är infrastrukturell eller adapterrelaterad ska den inte uttryckas i `Throw`
- Om auktorisering senare visar sig vara domänsemantik ska en separat ADR definiera en uttrycklig `DomainAuthorizationException` och kontextspecifika fabriker

### 9. Råa CLR/BCL-undantag är fortsatt tillåtna i teknisk infrastruktur

Interna tekniska komponenter som generatorer, expression rewriters och annan infrastruktur får använda CLR/BCL-undantag där ingen domänsemantik korsar API-gränsen.

### 10. `Throw` behålls som syntaktisk och verbal fasad

Namnet `Throw` är avsiktligt och ska behållas.

Det motiveras på tre grunder:
- som direkt korrelation till C#-syntaxen `throw`
- som verb, inte substantiv
- som markör för att ett semantiskt fel inträffar nu och avbryter exekveringen

Domänsemantiken uttrycks i de nestade begreppen och fabriksnamnen, medan `Throw` utgör den kanoniska handlingen.

Exempel:
- `Throw.Tidsrymd.SlutFöreStart(...)`
- `Throw.Personnummer.OgiltigtPersonnummer(...)`
- `Throw.Vårdval.ÖverlappEjTillåtet(...)`

Regel:
- Alla publika medlemmar under `Throw` ska kasta direkt
- `Throw` får inte användas för icke-kastande API:er

## Konkreta API- och fabrikskonsekvenser

### `Personnummer`

Nuvarande publika API:
- `Parse`
- `TryParse`

Ska ersättas med:
- `Tolka(string input)`
- `FörsökTolka(string? input, out Personnummer p)`
- `FörsökTolka(string? input, DateOnly referenceDate, out Personnummer p)`

Felsemantik:
- `Throw.Personnummer.OgiltigtPersonnummer(string raw)`  
  ska kasta `DomainArgumentFormatException`

### `HsaId`

Publikt API är redan semantiskt korrekt:
- `Tolka`
- `FörsökTolka`

Felsemantik:
- `Throw.HsaId.Ogiltigt(string raw)`  
  ska kasta `DomainArgumentFormatException`

### Nya fabriker för `Tidsrymd`

- `Throw.Tidsrymd.SlutFöreStart(DateTimeOffset start, DateTimeOffset? slut)`  
  kastar `DomainValidationException`
- `Throw.Tidsrymd.StegMåsteVaraPositivt(TimeSpan steg)`  
  kastar `DomainValidationException`
- `Throw.Tidsrymd.VaraktighetOdefinieradFörTillsvidare()`  
  kastar `DomainUndefinedOperationException`
- `Throw.Tidsrymd.StegningKräverAvgränsatIntervall()`  
  kastar `DomainUndefinedOperationException`
- `Throw.Tidsrymd.EnumereringKräverAvgränsatIntervall()`  
  kastar `DomainUndefinedOperationException`
- `Throw.Tidsrymd.OmfångKräverMinstEttIntervall()`  
  kastar `DomainUndefinedOperationException`
- `Throw.Tidsrymd.DatumintervallKräverAvgränsatIntervall()`  
  kastar `DomainUndefinedOperationException`

### Nya fabriker för `Datumintervall`

- `Throw.Datumintervall.SlutFöreStart(DateOnly start, DateOnly? slut)`  
  kastar `DomainValidationException`
- `Throw.Datumintervall.EnumereringKräverAvgränsatIntervall()`  
  kastar `DomainUndefinedOperationException`

## Designregler för fabriker

Varje fabrik ska:
- ligga nära det begrepp vars semantik den uttrycker
- ha en kod av formen `"{nameof(Begrepp)}.{nameof(Fabrik)}"`
- använda domäntermer i meddelandet
- bära relevanta värden när det stärker förklarbarheten
- ha exakt en kanonisk benämning per felbegrepp

Exempel:

```csharp
public sealed class DomainUndefinedOperationException(string code, string message)
    : DomainException(code, message);

public static class Tidsrymd
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void VaraktighetOdefinieradFörTillsvidare()
        => throw new DomainUndefinedOperationException(
            $"{nameof(Tidsrymd)}.{nameof(VaraktighetOdefinieradFörTillsvidare)}",
            "Varaktighet är odefinierad för tillsvidare-intervall.");
}
```

## Alternativ som avvägts

### Alternativ A: Behåll råa CLR/BCL-undantag i värdeobjekt och behåll `Parse` / `TryParse`

Avvisas.  
Detta bryter mot ADR-003, ADR-004 och ADR-005, och bibehåller både exceptionell och språklig inkonsekvens.

### Alternativ B: Behåll `Parse` / `TryParse` men åtgärda bara exceptions

Avvisas.  
Det minskar en del teknisk risk men lämnar en semantiskt inkonsekvent publik domänyta där likartade värdeobjekt uttrycker sig med olika verb utan motiverad skillnad.

### Alternativ C: Byt till svenska tolkningsverb men återanvänd bara befintliga exceptiontyper

Delvis avvisas.  
Namnfrågan löses, men odefinierade operationer riskerar fortsatt att felklassas som valideringsfel eller tillståndsövergångar.

### Alternativ D: Svenska tolkningsverb + en separat kategori för odefinierade domänoperationer

Accepteras.  
Detta ger både språklig och semantisk konsistens.

## Konsekvenser

### Fördelar

- Domänens publika yta blir konsekvent svensk
- `Personnummer` och `HsaId` uttrycker samma sorts ansvar med samma sorts verb
- Värdeobjektens felvokabulär blir enhetlig och verifierbar
- Referensarkitekturen blir mer självkonsistent mot sina egna ADR:er
- Delivery kan skilja bättre mellan domänfel och tekniska fel
- Kod, dokumentation och tester får samma språk

### Nackdelar

- Bryter med .NET-standardnamn som `Parse` och `TryParse`
- Kräver uppdatering av anropande kod, tester, XML-dokumentation och EF-konfiguration
- Introducerar ytterligare en exceptiontyp
- Kräver disciplin i gränsdragningen mellan formatfel, valideringsfel och odefinierad operation

## Implementationsstatus

Genomfört:
- `DomainUndefinedOperationException` tillagd i `DomainExceptions.cs`
- `Throw.Tidsrymd` (7 fabriker) och `Throw.Datumintervall` (2 fabriker) implementerade
- `Tidsrymd.cs` och `Datumintervall.cs` använder Throw-fabriker — inga råa CLR/BCL-undantag kvar
- `Personnummer.Parse` → `Tolka`, `TryParse` → `FörsökTolka` — alla anrop uppdaterade
- `HsaId` och `Personnummer` har nu konsekvent publik semantik (`Tolka`/`FörsökTolka`)
- Delivery-mappning utökad med `DomainUndefinedOperationException` → 422
- Alla nya fabriker verifierade i `ThrowVerifications.cs`

## Relaterade dokument

- `docs/adr/ADR-002 Semantic names for tests.md`
- `docs/adr/ADR-003 Domain failure vocabulary.md`
- `docs/adr/ADR-004 Semantic precision in exception factories.md`
- `docs/adr/ADR-005 Verification of domain failure semantics.md`
- `docs/adr/ADR-006 Delivery translates domain failure semantics to transport semantics.md`
- `docs/adr/ADR-007 Dual failure channel.md`
- `docs/guides/POLICY.md`
- `docs/guides/KODERGONOMI.md`
