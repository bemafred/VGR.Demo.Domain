
# VGR Analyzer Rules

- **VGR001** – Domän-egenskaper får inte ha public `set`. Exponera read-only och mutera via beteenden/fabriker.
- **VGR002** – Exponera inte muterbara samlingar i domänen (`ICollection<T>`, `IList<T>`, `List<T>`). Använd privat backing + `IReadOnlyList<T>`.

Reglerna träffar endast kod i namnrymder som innehåller `VGR.Domain`.
