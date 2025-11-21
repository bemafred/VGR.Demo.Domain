# Namnbrief: Epistemic Clean (E-Clean) & Semantic Architecture

**Primärbenämningar:**
- **Epistemic Clean Architecture (E-Clean)** — principerna och mönstret
- **Semantic Architecture** — implementationen av principerna

**Svenska:**
- **Epistemisk Ren Arkitektur**
- **Semantisk Arkitektur**

---

# 1. Vad är vad?

**E-Clean definierar hur vi bygger:** principer, mönster, ren domän, separation av lager, teknik som översätter språket.

**Semantic Architecture definierar vad vi bygger:** det semantiska system som implementerar principerna — attribut, expansioner, kataloger, uttrycksträd, projektioner och språkligt API.

Kort sagt:

> **E-Clean är principerna. Semantic Architecture är implementationen.**

Precis som MVC är ett mönster och ASP.NET MVC är en implementation.

---

# 2. Tagline

**Language is the interface. Semantics execute.**  
(Sv: *Språket är gränssnittet. Semantiken exekverar.*)

---

# 3. Epistemic Clean Architecture (E-Clean)

## 3.1 Definition
*E-Clean är ett arkitekturmönster som bygger på en ren, rik domän; tydlig separation mellan domän och teknik; och kodergonomi genom språkliga uttryck istället för tekniska artefakter.*

## 3.2 Principerna
- Ren och isolerad domän
- Domänspråket är primärt – teknik översätter språket
- Infrastruktur som translation
- Invariants via exceptions
- Outcome i tekniklagret
- Enkelhet först
- Kodergonomi och navigerbarhet som huvudmål

## 3.3 Mönstren
- Interaktorer (kommandon och queries)
- Semantiska uttryck istället för DTO:er
- Tydliga moduler och värdeobjekt
- Projektioner i Domain.Queries
- Expansioner i Semantics.Linq

---

# 4. Semantic Architecture
*(Implementation av E-Clean)*

## 4.1 Definition
*Semantic Architecture är den konkreta implementationen av E-Clean: semantiska attribut, genererade expansioner, Semantic Registry, uttrycksträd, projektioner och maskinläsbar katalog.*

## 4.2 Komponenter
- **Semantic Registry** – katalog över semantik, uttryck, samband
- **Semantiska attribut** – `SemanticQueryAttribute`, `ExpansionFor`, m.fl.
- **Generatorbaserad expansion** – C#-källkod skapas från semantik
- **Semantics.Linq** – språkligt API för domänuttryck
- **Domain.Queries** – projektioner och formationer över data
- **Uttrycksöversättning** – semantik → SQL/EF
- **Visualisering** – levande domänmodell
- **AI-förståelse** – semantik som stabilt API för kodgenerering

---

# 5. Hur delarna hör ihop

| E-Clean (mönster) | Semantic Architecture (implementation) |
|-------------------|----------------------------------------|
| Principer | Konkreta attribut, generators, katalog |
| Teori | Kod |
| Ren domän | Språkligt domän-API |
| Lager & separation | Semantic Registry och expansion |
| Interaktorer | Semantikdriven exekvering |
| Struktur | Mening |
| Arkitektur | Plattform |

**Sammanfattning:**
> **E-Clean är arkitekturen. Semantic Architecture är den semantiska maskin som implementerar den.**

---

# 6. Hisspitcher

### För utvecklare
*E-Clean är mönstret du känner igen. Semantic Architecture är implementationen som gör mönstret uttrycksfullt och exekverbart.*

### För verksamhet
*Era regler och begrepp blir ett språk som både människor och maskiner kan läsa, förstå och exekvera.*

### För AI
*Ni får ett stabilt, semantiskt API att generera korrekt kod från — utan gissningar.*

---

# 7. Kortversion

> **E-Clean = principerna.  
> Semantic Architecture = implementationen.**  
> Tillsammans bildar de en epistemisk arkitektur där språket är gränssnittet och semantiken exekverar.