# ADR-004: Semantisk precision i undantagsfabriker

## Status
Accepterad

## Kontext
I denna arkitektur är namn inte kosmetik utan bärande semantik.
En undantagsfabrik är ett domänuttryck.
Om namnet inte exakt motsvarar den regel eller tillståndsövergång som brutits uppstår semantisk drift även när implementationen fungerar tekniskt.

Vi behöver därför ett beslut som skyddar precisionen i domänens felvokabulär.

## Beslut
- Namnet på varje undantagsfabrik ska uttrycka exakt den domänregel som brutits.
- `nameof` används som primär mekanism för semantiska identifierare.
- Det får endast finnas en kanonisk benämning per felbegrepp.
- Överlappande, dubblerade eller otydliga undantagsfabriker ska konsolideras.
- XML-dokumentation, tester och adaptermappning ska använda samma vokabulär som domänen.

## Konsekvenser

### Fördelar
- Domänen blir mer självbärande som semantiskt system.
- Refaktoriseringar bevarar mening i stället för att sprida alternativa termer.
- Undantagsytan blir ett förstaklassigt uttryck för domänkunskap.

### Nackdelar
- Existerande fabriksnamn och kommentarer kan behöva justeras för att nå full precision.
- Oanvända eller dubblerade fabriker kan behöva tas bort även om de känns bekväma att ha kvar.

## Kända avvikelser

Följande brister mot beslutets principer har identifierats i den nuvarande implementationen:

1. **Dubbletter**: `Throw.Person.OgiltigtPersonnummer` och `Throw.Personnummer.OgiltigtPersonnummer` uttrycker samma felbegrepp. Bör konsolideras till en kanonisk plats.
2. **Felaktigt parameternamn**: `Throw.Person.Saknas(PersonId regionId)` — parametern heter `regionId` men typen är `PersonId`. Bryter mot principen att namn bär mening.
3. **Semantisk feltyp**: `Throw.Person.HittadesInte` kastar `DomainInvalidStateTransitionException` men uttrycker att ett aggregat saknas — bör vara `DomainAggregateNotFoundException` (eller konsolideras med `Person.Saknas`).
4. **Oklara koder**: `Concurrency.Conflict` och `Idempotency.Duplicate` har TODO-kommentarer om att koden inte är bra. Behöver domänförankring.

## Relaterade dokument
- `docs/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/KODERGONOMI.md`
- `docs/PLACERING.md`
