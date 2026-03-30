# ADR-005: Felsemantik är verifierbar domänbeteende

## Status
Accepterad

## Kontext
Som referensarkitektur måste lösningen inte bara modellera rätt semantik utan också verifiera den.
Domänens felsemantik är en del av dess beteende och får inte behandlas som en bieffekt.

Vi behöver därför ett beslut som gör undantagsytan verifierbar på samma sätt som andra domänregler.

## Beslut
- Varje använd undantagsfabrik i `Throw` ska ha minst en verifiering i domänens verifieringsprojekt.
- Verifieringar ska täcka undantagstyp, semantisk identifierare och representativt scenario.
- Oanvända undantagsfabriker ska antingen verifieras genom uttryckligt reserverade scenarier eller tas bort.
- Förekomst av BCL-undantag i domänen ska vara explicit motiverad och verifierad.

## Konsekvenser

### Fördelar
- Domänens felvokabulär blir en exekverbar del av arkitekturen.
- Taxonomisk drift blir lättare att upptäcka.
- Referensrepo:t blir starkare som pedagogiskt och arkitektoniskt artefakt.

### Nackdelar
- Verifieringsytan ökar.
- Introduktion av nya undantagsfabriker kräver mer disciplin i testskrivandet.

## Kända avvikelser

Flera undantagsfabriker i `Throw` saknar idag verifieringstester:
- `Throw.Användare.EjAuktoriserad`
- `Throw.Person.VårdvalSaknas`, `Throw.Person.HittadesInte`
- `Throw.Vårdval.AktivtFinnsRedan`, `Throw.Vårdval.RedanAvslutat`, `Throw.Vårdval.VårdvalGällerIntePåHsaId`
- `Throw.Vårdcentral.LäkareEjValbarPåEnhet`
- `Throw.HsaId.MappningSaknas`
- `Throw.Concurrency.Conflict`, `Throw.Idempotency.Duplicate`

Dessa bör antingen verifieras eller motiveras som reserverade för framtida scenarier, i enlighet med beslutet.

## Relaterade dokument
- `docs/ADR-000 E-Clean & Semantic Architecture.md`
- `docs/POLICY.md`
- `docs/ONBOARDING.md`
