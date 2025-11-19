using System;
using System.Linq.Expressions;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Abstractions;

namespace VGR.Infrastructure.EF.Expansions;

public static class TidsrymdExpansions
{
    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.Innehåller))]
    public static Expression<Func<Tidsrymd, DateTimeOffset, bool>> Innehåller_Expansion()
        => (r, t) => r.Start <= t && (r.Slut == null || t < r.Slut);

    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.Överlappar))]
    public static Expression<Func<Tidsrymd, Tidsrymd, bool>> Överlappar_Expansion()
        => (a, b) =>
            a.Start < (b.Slut ?? DateTimeOffset.MaxValue) &&
            b.Start < (a.Slut ?? DateTimeOffset.MaxValue);

    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.ÄrTillsvidare))]
    public static Expression<Func<Tidsrymd, bool>> ÄrTillsvidare_Expansion()
        => (a) => a.Slut == null;
}
