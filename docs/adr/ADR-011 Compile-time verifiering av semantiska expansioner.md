# ADR-011: Compile-time verifiering av semantiska expansioner

## Status
Genomförd

## Kontext

Domänmetoder markerade med `[SemanticQuery]` (t.ex. `Tidsrymd.Innehåller`, `Tidsrymd.Överlappar`)
kräver en motsvarande `[ExpansionFor]`-metod i infrastrukturlagret för att kunna översättas till SQL
via `SemanticRegistry`. Om en expansion saknas misslyckas EF Core-frågan vid runtime — ett fel som
borde vara möjligt att upptäcka vid compile-time.

### Nuläge

`VGR.Semantics.Generator` innehåller en Roslyn incremental generator med två diagnostiker:

- **SEMANTICS001** — `[SemanticQuery]` utan matchande `[ExpansionFor]` (Error)
- **SEMANTICS002** — `[ExpansionFor]` utan matchande `[SemanticQuery]` (Warning)

Generatorn refereras som analyzer i tre projekt (`VGR.Semantics.Linq`, `VGR.Application`, `VGR.Web`),
men är **helt inert** i samtliga:

1. `[SemanticQuery]`-attributen lever i `VGR.Domain` — ingen av de tre kompilationerna har dessa
   som syntax trees, och generatorns `SyntaxProvider`-baserade scanning hittar dem inte.
2. `[ExpansionFor]`-attributen lever i `VGR.Infrastructure.EF` — inte heller synliga som syntax trees
   i alla tre kompilationerna.
3. `GenerateRegistry()` (75 rader kodgenerering med AOT-säkra MethodInfo-cachear) anropas aldrig.
   Runtime reflection i `SemanticRegistry.DiscoverExpansions()` hanterar all wiring.

Generatorn kompilerar, installeras, men producerar aldrig en diagnostik eller genererad fil.

### Rotorsak

Source generators kan bara se syntax trees i sin egen kompilation. `[SemanticQuery]` och
`[ExpansionFor]` lever i olika projekt. ChatGPT löste runtime-problemet genom att introducera
reflection-baserad discovery men lämnade den inerta generatorn på plats — ett TGSP-liknande
resonemangsfel.

## Beslut

### 1. Generatorn placeras i `VGR.Infrastructure.EF` — inte i Domain

`VGR.Infrastructure.EF` är det enda projektet som refererar *båda* sidor:
- `VGR.Domain` (via projektberoende) → `[SemanticQuery]`-attribut synliga som **metadata-symboler**
- Lokala filer → `[ExpansionFor]`-attribut synliga som **syntax trees**

Infrastrukturlagret har ansvaret att uppfylla domänens kontrakt. Valideringen att kontraktet
uppfylls hör därför hemma här — inte i domänen.

### 2. `[SemanticQuery]`-scanning redesignas till metadata-baserad

Nuvarande implementation använder `SyntaxProvider.CreateSyntaxProvider()` som bara hittar
syntax trees. `[SemanticQuery]` i Domain-typer måste hittas via:

```
compilation.GetTypeByMetadataName("VGR.Domain.SharedKernel.Tidsrymd")
  .GetMembers()
  .Where(m => m.GetAttributes().Any(a => a.AttributeClass == semanticQueryAttr))
```

Alternativt skannar generatorn alla refererade assemblies metadata-typer.

### 3. `[ExpansionFor]`-scanning behålls som SyntaxProvider

`[ExpansionFor]`-metoder lever lokalt i Infrastructure.EF och hittas via syntax trees som idag.

### 4. Matchning och diagnostik

Generatorn matchar de två uppsättningarna:
- `{Typ.Medlem}` från `[SemanticQuery]` (metadata)
- `{TargetType.TargetMethod}` från `[ExpansionFor]` (syntax)

Rapporterar:
- **SEMANTICS001** — `[SemanticQuery]`-medlem utan matchande `[ExpansionFor]` (Error)
- **SEMANTICS002** — `[ExpansionFor]` som pekar på icke-`[SemanticQuery]`-medlem (Warning)

Feedback syns i realtid i Rider/Visual Studio/VS Code.

### 5. `GenerateRegistry()` tas bort

Den döda kodgenereringen (AOT MethodInfo-cache, genererad statisk konstruktor) tas bort.
Runtime reflection i `SemanticRegistry.DiscoverExpansions()` behålls som enda wiring-mekanism.
Kodgenerering kan återintroduceras senare om AOT-krav uppstår.

### 6. Generator-referenser tas bort från övriga projekt

`VGR.Semantics.Linq`, `VGR.Application` och `VGR.Web` behöver inte generatorn.
Endast `VGR.Infrastructure.EF` refererar den.

## Konsekvenser

- **Positiva:** Compile-time feedback vid saknade expansioner. IDE-integration. Eliminierar
  en kategori av runtime-fel. Städar bort 75 rader död kod och tre inerta analyzer-referenser.
- **Negativa:** Metadata-scanning av refererade assemblies kan vara långsammare än SyntaxProvider.
  Acceptabelt — det sker vid compile-time, inte runtime.
- **Invariant:** Runtime reflection i `SemanticRegistry` behålls oförändrad. Generatorn är
  enbart diagnostik — den påverkar inte runtime-beteende.
