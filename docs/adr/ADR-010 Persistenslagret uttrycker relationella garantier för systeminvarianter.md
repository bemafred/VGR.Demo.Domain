# ADR-010: Persistenslagret uttrycker relationella garantier för systeminvarianter

## Status
Föreslagen

## Kontext

ADR-000 fastslår att applikationslagret ska uttrycka use cases som domänprosa och att infrastrukturen ska lära sig domänens språk, inte tvärtom.  
ADR-001 fastslår att databasen ska stödja semantiska frågor genom explicit indexering.  
ADR-009 skärper kraven på produktionshärdning, inklusive samtidighetskontroll och robust felhantering.

Den här lösningen är medvetet utformad för **pushdown**, **selektiv hydrering** och **minsta nödvändiga dataåtkomst**.  
Det är alltså **inte** ett mål att alltid ladda hela aggregat enbart för att “låta domänen avgöra allt i minnet”.

Tvärtom bygger arkitekturen på att:

- domänen äger regelns **semantik**
- applikationslagret väljer **verkställighetsstrategi**
- persistenslagret uttrycker de **relationella garantier** som måste hålla även under samtidighet och flera skrivare

En granskning av nuvarande lösning visar dock att vissa regler ännu inte är fullt deklarerade i EF-modellen:

1. Vissa invariants uttrycks i praktiken via laddningsstrategi i applikationslagret, men saknar motsvarande databasnivåskydd.
2. Concurrency-konfigurationen är ännu inte fastslagen som en kanonisk, provider-medveten strategi.
3. Det finns en risk att relationella garantier blandas ihop med testtekniska adapterlösningar.

Vi behöver därför tydligt besluta:

- när selektiv hydrering är korrekt
- vilka invariants som måste ha ett sista skydd i den relationella modellen
- hur providerspecifik EF-konfiguration ska förhålla sig till testharness och produktion

## Problem

Utan ett explicit beslut riskerar lösningen att glida in i ett oklart mellantillstånd:

- applikationslagret blir bärare av implicita persistensantaganden
- domänregler som kräver store-wide korrekthet saknar hårt sista skydd
- samtidighetsfrågor reduceras till “bästa försök” i stället för deklarerad policy
- testadapterlogik misstolkas som produktionskrav

Detta är inte ett argument för full hydrering som norm.  
Det är ett argument för att **enforcement-strategin måste vara explicit**.

## Beslut

### 1. Full hydrering är inte standardstrategi

Det är **inte** ett självändamål att ladda hela aggregat för att upprätthålla domänens regler.

Standardprincipen i denna lösning är fortsatt:

- pushdown där regeln eller förkontrollen kan uttryckas semantiskt i fråga
- selektiv hydrering av precis den del av aggregatet som mutation kräver
- databaskonstraint eller concurrency-kontroll som sista skydd där store-wide korrekthet krävs

### 2. Applikationslagret väljer verkställighetsstrategi, inte regelns innebörd

Domänen fortsätter att äga regelns semantik.  
Applikationslagret ansvarar för att välja den mest ändamålsenliga verkställighetsformen för use caset.

Tillåtna strategier är:

- semantisk SQL-pushdown
- explicit precheck i fråga före mutation
- selektiv laddning av relevanta navigationer
- relationell constraint/index
- explicita concurrency-mekanismer

Det är förbjudet att låta korrekthet vila på en **implicit** förutsättning om hur mycket av aggregatet som råkar vara laddat, om regeln i själva verket kräver store-wide garanti.

### 3. Persistenslagret ska uttrycka systeminvarianter som måste hålla under samtidighet

Invariants som måste vara sanna oberoende av:

- antal parallella requests
- antal processer/noder
- exakt laddningsstrategi i applikationslagret

ska uttryckas som relationella garantier i persistenslagret där det är praktiskt möjligt.

Detta gäller särskilt:

- unika identitets- och dubblettregler
- “högst en öppen rad” eller motsvarande partiella unikhetsregler
- foreign keys och andra strukturregler

### 4. Konkreta EF-konfigurationskonsekvenser

Följande justeringar ska betraktas som kanoniska mål för den relationella modellen:

#### Person

Om regeln kvarstår att ett `Personnummer` bara får förekomma en gång per `Region`, ska EF-modellen uttrycka detta med ett **unikt index/constraint** över:

- `RegionId`
- `Personnummer`

Applikationslagret får fortfarande göra billig pushdown-check för snabb respons, men databasen ska vara sista skyddet mot samtidighetsfel.

#### Vårdval

Om regeln är att en person bara får ha **ett öppet/aktivt vårdval åt gången**, ska EF-modellen uttrycka detta med ett **filtrerat unikt index** över `PersonId` där `Slut IS NULL` eller providerspecifik motsvarighet.

Detta ersätter inte domänens semantik, men det gör den relationella garantin explicit.

#### Historiska överlapp

Om en regel går längre än “högst ett öppet vårdval” och i stället förbjuder **historiska överlapp över hela tidslinjen**, ska detta **inte** lösas genom att göra full hydrering till standard.

I sådant fall ska regeln uttryckas genom någon av följande explicita strategier:

- semantisk precheck i fråga före mutation
- providerspecifik databasfunktionalitet om sådan väljs
- annan uttrycklig persistence policy definierad i separat ADR

### 5. Concurrency-strategin ska vara explicit och provider-medveten

En concurrency token är inte tillräcklig bara för att den råkar vara markerad i EF.

För produktionsprovider ska en kanonisk strategi väljas och dokumenteras:

- på SQL Server: riktig `rowversion` / `IsRowVersion()`
- på andra providers: dokumenterad och avsiktlig motsvarighet, exempelvis app-hanterad versionskolumn

En generisk placeholder-konfiguration utan tydlig providerstrategi ska inte betraktas som slutligt produktionsmönster.

### 6. Providerspecifik EF-konfiguration hör hemma i infrastruktur, inte i domän

Providerspecifika skillnader i lagring, index, konverteringar och concurrency hör hemma i `Infrastructure.EF` eller motsvarande runtime-infrastruktur.

Detta gäller exempelvis:

- filtered indexes
- providerberoende value converters
- `rowversion` kontra app-hanterad version
- SQL-dialektspecifika index- eller constraint-uttryck

### 7. Testharnessens adapterlogik är inte produktionskrav

Providerspecifik logik som enbart existerar för att möjliggöra korrelationstester mot en begränsad testprovider ska stanna i testharnessen.

Exempel:

- SQLite-specifika konverteringar för `DateTimeOffset` som endast behövs för att få jämförelser att fungera i testmiljö

Sådan logik är en testadapter, inte en del av produktionsinfrastrukturen, och ska inte användas som argument för hur runtime-konfigurationen måste se ut.

### 8. EF-konfiguration ska vara explicit när domänmodellen redan är explicit

När en property redan finns i domänmodellen ska EF-konfigurationen i första hand referera den explicit och typat.

Strängbaserad eller shadow-baserad konfiguration ska reserveras för fall där modellen verkligen kräver det.

Målet är:

- mindre skör mapping
- tydligare läsbarhet
- bättre spårbarhet mellan domän och persistens

## Konsekvenser

### Fördelar

- Arkitekturen behåller pushdown och selektiv hydrering som förstahandsstrategi
- Systeminvarianter som kräver relationell garanti blir explicit deklarerade
- Samtidighetsproblem fångas där de faktiskt uppstår
- Skillnaden mellan domänsemantik, applikationspolicy och persistensgaranti blir tydlig
- Testharnessens adapteransvar hålls separerat från produktionsinfrastruktur

### Nackdelar

- Fler migrations och mer providerspecifik EF-konfiguration
- Filtered indexes och concurrency-strategier kan kräva olika implementation per provider
- Vissa regler kan behöva dubbelt uttryck: snabb precheck i applikationslagret och hårt skydd i databasen

## Avgränsning

Detta ADR beslutar **inte**:

- att hela aggregat alltid ska laddas
- att alla domänregler måste översättas till databaskonstraints
- att SQLite-testadapter ska fungera som produktionsreferens
- att historiska intervallöverlapp måste lösas med exakt en specifik databasteknik

Detta ADR beslutar **endast** att relationella garantier, concurrency-strategi och ansvarsfördelning mellan applikation och persistens ska vara explicita.

## Relaterade dokument

- `docs/adr/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/adr/ADR-001 Index.md`
- `docs/adr/ADR-007 Dual failure channel.md`
- `docs/adr/ADR-009 Produktionshärdning av delivery- och infrastrukturlager.md`
- `docs/appendix/APPENDIX J - Performance & Query Optimization.md`
