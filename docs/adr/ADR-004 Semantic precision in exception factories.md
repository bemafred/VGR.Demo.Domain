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

## Tidigare kända avvikelser (åtgärdade)

1. **Dubbletter**: `Throw.Person.OgiltigtPersonnummer` duplicerade `Throw.Personnummer.OgiltigtPersonnummer`. Åtgärd: konsoliderad till en kanonisk plats under `Personnummer`.
2. **Felaktigt parameternamn**: `Throw.Person.Saknas(PersonId regionId)` — parametern hette `regionId`. Åtgärd: korrigerad till `personId`.
3. **Semantisk feltyp**: `Throw.Person.HittadesInte` kastade fel undantagstyp och duplicerade `Saknas`. Åtgärd: borttagen — `Throw.Person.Saknas` är kanonisk fabrik för saknat Person-aggregat.
4. **Oklara koder**: `Concurrency.Conflict` och `Idempotency.Duplicate` hade TODO-kommentarer. Åtgärd: koderna använder `nameof`-mönstret konsekvent, TODO:er borttagna.

## Relaterade dokument
- `docs/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/KODERGONOMI.md`
- `docs/PLACERING.md`
