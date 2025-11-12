# VGR.QuerySemantics

A tiny expression-rewriter to keep your domain semantics while making EF see plain LINQ.

## Register

```csharp
var Semantik = new QuerySemantics()
  .Register<Tidsrymd, DateTimeOffset, bool>(
    (p, t) => p.Innehåller(t),
    (p, t) => p.Start <= t && (p.Slut == null || t < p.Slut))
  .Register<Tidsrymd, Tidsrymd, bool>(
    (a, b) => a.Överlappar(b),
    (a, b) => a.Start <= (b.Slut ?? DateTimeOffset.MaxValue)
           && b.Start <= (a.Slut ?? DateTimeOffset.MaxValue));
```

## Use

```csharp
var aktiva = db.Vårdval.WithSemantics(Semantik)
                       .Where(v => v.Period.Innehåller(clock.UtcNow));
```
