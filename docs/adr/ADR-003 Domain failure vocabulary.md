# ADR-003: Domänens semantiska felvokabulär

## Status
Accepterad

## Kontext
E-Clean slår fast att domänspråket är primärt och att invariants uttrycks via exceptions.
I denna arkitektur är `Throw` därför inte ett tekniskt hjälplager utan domänens semantiska undantagsfabriker.

När domänsignifikanta fel uttrycks delvis via `Throw` och delvis via generella CLR-undantag uppstår semantisk splittring:
- domänen talar flera felspråk
- delar av felhanteringen blir tekniska i stället för domänmässiga
- referensarkitekturen blir mindre självbärande

Vi behöver därför ett uttryckligt beslut om att domänens fel är en del av domänens vokabulär.

## Beslut
- `Throw` utgör domänens kanoniska felvokabulär.
- Alla domänsignifikanta fel uttrycks via `DomainException`-hierarkin och namngivna fabriker i `Throw`.
- CLR/BCL-undantag används endast för rent tekniska programmeringskontrakt där ingen domänsemantik avses.
- Värdeobjekt omfattas av samma princip som aggregat.

## Konsekvenser

### Fördelar
- Domänen talar ett enhetligt felspråk.
- Fel blir navigerbara, refaktorerbara och semantiskt begripliga.
- Referensarkitekturen blir tydligare för utvecklare, analyzers och AI-stöd.

### Nackdelar
- Fler domänspecifika undantagsfabriker kan behöva införas.
- Gränsen mellan domänsignifikanta fel och tekniska kontraktsfel måste hållas tydlig.

## Tidigare kända avvikelser (åtgärdade)

- `Throw.Användare.EjAuktoriserad` kastade `UnauthorizedAccessException` (BCL). Åtgärd: borttagen — auktorisering är inte domänsemantik i denna lösning (se ADR-008 §8).
- `Throw.Person.OgiltigtPersonnummer` duplicerade `Throw.Personnummer.OgiltigtPersonnummer`. Åtgärd: konsoliderad till `Throw.Personnummer.OgiltigtPersonnummer`.

## Relaterade dokument
- `docs/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/ARCHITECTURE-NAME.md`
- `docs/POLICY.md`
