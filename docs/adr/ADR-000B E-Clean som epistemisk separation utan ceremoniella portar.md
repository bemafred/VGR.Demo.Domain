# ADR-000B: E-Clean som epistemisk separation utan ceremoniella portar

## Status
Föreslagen variant

## Kontext

ADR-000 etablerar **Epistemic Clean Architecture (E-Clean)** och **Semantic Architecture** som lösningens grundmönster.  
Dokumentet är starkt som deklaration av domänens suveränitet, semantisk persistens och kodergonomi.

Det finns dock ett återkommande tolkningsproblem:

- formuleringar som **"Ports & Adapters"** läses lätt genom klassisk Clean/Hexagonal Architecture
- läsare förväntar sig därför fysiska portlager, repository-abstraktioner och tekniskt separerade interface-gränser
- när dessa lager inte finns explicit tolkas det som kompromiss, skuld eller ofullständig implementation

Detta är en feltolkning.

I denna arkitektur löses separationen i första hand **epistemiskt**, inte ceremoniellt.  
Det avgörande är inte hur många tekniska lager som står mellan komponenter, utan:

- var sanningen uttrycks
- var semantik översätts
- var verkställighetsstrategin väljs
- hur alternativ sanning förhindras

Denna lösning är därför en **edge architecture** som medvetet tänjer gränserna för etablerade mönster.  
Den ska inte utvärderas med standardheuristiker som:

- "Application får inte känna till EF"
- "Ports måste vara interfaces"
- "Alla regler ska verkställas via full aggregatladdning"
- "Fler lager ger bättre separation"

## Problem

När arkitekturen läses med klassiska reflexer uppstår återkommande felbedömningar:

1. **Konceptuell separation misstolkas som teknisk brist**  
   Direkt beroende från `Application` till `Infrastructure.EF` läses som läckage, trots att separationen i praktiken upprätthålls genom domänens epistemiska suveränitet och semantisk översättning.

2. **Verkställighetsstrategi blandas ihop med domänens sanning**  
   När en regel inte verkställs via full hydrering antas det att regeln är felplacerad eller ofullständigt modellerad.

3. **Ceremoni premieras över klarhet**  
   Förslag om repositories, explicita portar och fler adapterlager uppstår trots att de inte tillför ny semantisk disciplin.

4. **AI-verktyg faller tillbaka till mainstream-mönster**  
   När dokumentationen inte uttryckligen förbjuder dessa default-reflexer kommer verktyg att försöka "normalisera" lösningen mot etablerad standardarkitektur.

Vi behöver därför ett uttryckligt förtydligande av vad E-Clean **är**, och lika viktigt, vad det **inte** är.

## Beslut

### 1. E-Clean är epistemisk separation, inte lagerceremoni

E-Clean definierar separation primärt genom **kunskapsansvar**, inte genom obligatoriska tekniska mellanlager.

Det betyder:

- domänen äger sanningen
- semantiklagret äger översättbarheten
- applikationslagret äger verkställighetsstrategin
- infrastrukturen äger persistens- och providerbeteende

Om dessa ansvar hålls rena är arkitekturen korrekt även utan klassiska repository-/portlager.

### 2. "Ports & Adapters" är här ett konceptuellt, inte nödvändigtvis fysiskt mönster

I denna kodbas betyder "Ports & Adapters" inte att varje gräns måste materialiseras som:

- separat interface
- separat adapterklass
- separat repository-lager

Det betyder i stället att:

- domänen inte ska bära infrastrukturkunskap
- alternativa sanningar inte ska uppstå i mellanlager
- tekniska översättningar ska hållas utanför domänens sanningsyta

En fysisk port är tillåten när den behövs. Den är inte norm i sig.

### 3. Applikationslagret är ett verkställighetsstrategiskt lager

`Application` ska inte förstås enbart som ett orkestreringslager eller service-lager i klassisk mening.

I denna arkitektur ansvarar applikationslagret också för att välja:

- vilken kunskap som måste hämtas
- i vilket exekveringssätt regeln ska tillämpas
- hur lite data som behöver laddas för att bevara korrekthet

Detta är ett epistemiskt ansvar, inte bara ett tekniskt.

### 4. Full hydrering är inte default

Det är inte ett självändamål att ladda hela aggregat för att domänregler ska "få lov" att exekvera.

Korrekt verkställighet kan uppnås genom:

- semantisk pushdown
- selektiv hydrering
- precheck i fråga
- relationell constraint
- concurrency-kontroll
- full hydrering när det faktiskt är rätt strategi

Frågan är därför inte:

> "Har hela aggregatet laddats?"

utan:

> "Har regeln verkställts med minsta möjliga epistemiska läckage och tillräcklig korrekthetsnivå?"

### 5. Direkt beroende från Application till Infrastructure är inte i sig ett designfel

I denna arkitektur ska ett direkt beroende från `Application` till `Infrastructure.EF` inte automatiskt bedömas som ett brott mot arkitekturen.

Det är först ett problem om beroendet:

- introducerar alternativ affärssanning utanför domänen
- flyttar regelns innebörd från domänen till infrastrukturen
- gör use caset beroende av tekniska artefakter på ett sätt som försvagar domänspråket

Om beroendet i stället används för att verkställa domänens semantik effektivt och tydligt är det förenligt med arkitekturen.

### 6. Arkitekturen ska bedömas med andra frågor än mainstream-mönster

Följande frågor är normerande i denna lösning:

- Var uttrycks sanningen?
- Var uppstår semantisk drift?
- Har en alternativ sanning introducerats?
- Är verkställighetsstrategin explicit?
- Är den valda strategin proportionerlig mot regelns natur?
- Bevaras kodergonomin?

Följande frågor är inte normerande i sig:

- Finns ett repository-interface?
- Finns en fysisk port?
- Känner `Application` till EF?
- Är varje regel verkställd via full aggregatladdning?

### 7. Testadapter och runtime-infrastruktur ska hållas isär

Tekniska kompensationer som endast behövs för testprovider eller testharness får inte användas som referensmodell för runtime-arkitekturen.

Testadapterlogik är:

- legitim
- avsiktlig
- avgränsad

men den är inte i sig ett argument för hur produktionsinfrastrukturen måste struktureras.

### 8. AI och människor måste aktivt undertrycka standardreflexer i denna kodbas

Vid analys eller förändringsförslag ska följande default-reflexer betraktas som otillförlitliga tills motsatsen visats:

- "lägg till ett repository"
- "lägg till interfaces för att rena lagren"
- "ladda hela aggregatet"
- "flytta all korrekthetslogik till en entitet"
- "ta bort direkt infra-beroende bara för att ett lagerdiagram annars ser renare ut"

En förändring är bara motiverad om den minskar epistemisk drift, ökar förklarbarhet eller ger en verklig korrekthetsvinst.

## Designregler för tolkning av arkitekturen

När en läsare eller ett AI-verktyg utvärderar ett use case ska analysen följa denna ordning:

1. Identifiera om problemet gäller **sanning**, **översättning**, **verkställighetsstrategi** eller **persistensgaranti**.
2. Avgör om det finns en faktisk alternativ sanning, eller bara ett brott mot ett klassiskt mönster.
3. Föreslå inte nya lager innan det visats att problemet inte kan lösas inom den befintliga epistemiska modellen.
4. Bedöm alltid om en föreslagen abstraktion tillför semantisk disciplin eller bara teknisk ceremoni.

## Konsekvenser

### Fördelar

- Arkitekturen kan försvaras på sina egna premisser
- Falska positiva "arkitekturproblem" minskar
- AI-verktyg får bättre förutsättningar att inte normalisera bort lösningens edge-karaktär
- Diskussioner om korrekthet kan föras på rätt nivå: semantik, strategi och garanti
- Dokumentationen blir mer ärlig mot den faktiska implementationen

### Nackdelar

- Kräver högre arkitekturell mognad hos läsaren
- Gör lösningen mindre direkt igenkännbar för utvecklare tränade i standard-CA/DDD-mönster
- Ställer högre krav på explicit dokumentation när mainstream-konventioner avsiktligt bryts

## Relation till ADR-000

Detta dokument är en **förtydligande variant** av ADR-000.

Det ersätter inte grundtesen i ADR-000, men preciserar hur följande formuleringar ska förstås i denna kodbas:

- "Ports & Adapters"
- "Application / Interactors"
- "ren separation"

Om denna variant accepteras bör framtida revidering av ADR-000 antingen:

- integrera dessa förtydliganden direkt
- eller länka till ADR-000B som normerande tolkningsram för E-Clean i praktiken

## Relaterade dokument

- `docs/adr/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/adr/ADR-001 Index.md`
- `docs/adr/ADR-009 Produktionshärdning av delivery- och infrastrukturlager.md`
- `AI-GUIDANCE.md`
- `docs/guides/ONBOARDING.md`
- `docs/guides/PLACERING.md`
