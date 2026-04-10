# Quickstart

## Förutsättningar

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Bygg och testa

```bash
# Bygg hela lösningen
dotnet build

# Kör alla tester
dotnet test
```

## Starta webbapplikationen

```bash
dotnet run --project src/VGR.Web/VGR.Web.csproj
```

Navigera till:

| Yta | Beskrivning |
|-----|-------------|
| `/domain` | Levande visualisering av domänmodellen (aggregat, värdeobjekt, relationer) |
| `/api` | Översikt av alla registrerade API-endpoints |
| `/data` | Reflection-drivet CRUD-gränssnitt för domänobjekt |

## Nästa steg

- **ONBOARDING.md** — hur arkitekturen hänger ihop (domän → semantik → infrastruktur)
- **PLACERING.md** — vilka projekt som finns och var kod placeras
- **POLICY.md** — felhantering, CQRS-light och teststrategi
