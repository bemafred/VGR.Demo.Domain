using System;
using System.Linq.Expressions;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Abstractions;

namespace VGR.Infrastructure.EF.Expansions;

/// <summary>EF-kompatibla expression-expansioner för <see cref="Tidsrymd"/>-domänmetoder.</summary>
public static class TidsrymdExpansions
{
    /// <summary>Expansion: <see cref="Tidsrymd.Innehåller(DateTimeOffset)"/> → <c>Start &lt;= t AND (Slut IS NULL OR t &lt; Slut)</c>.</summary>
    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.Innehåller))]
    public static Expression<Func<Tidsrymd, DateTimeOffset, bool>> Innehåller_Expansion()
        => (r, t) => r.Start <= t && (r.Slut == null || t < r.Slut);

    /// <summary>Expansion: <see cref="Tidsrymd.Överlappar"/> → halvöppen överlappningskontroll med NULL-hantering.</summary>
    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.Överlappar))]
    public static Expression<Func<Tidsrymd, Tidsrymd, bool>> Överlappar_Expansion()
        => (a, b) =>
            (b.Slut == null || a.Start < b.Slut) &&
            (a.Slut == null || b.Start < a.Slut);

    /// <summary>Expansion: <see cref="Tidsrymd.ÄrTillsvidare"/> → <c>Slut IS NULL</c>.</summary>
    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.ÄrTillsvidare))]
    public static Expression<Func<Tidsrymd, bool>> ÄrTillsvidare_Expansion()
        => (a) => a.Slut == null;
}
