using System;
using System.Linq.Expressions;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Abstractions;

namespace VGR.Infrastructure.EF;

public static class TidsrymdExpansions
{
    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.Innehåller))]
    public static LambdaExpression Innehåller_Expansion()
    {
        var r = Expression.Parameter(typeof(Tidsrymd), "r");
        var t = Expression.Parameter(typeof(DateTimeOffset), "t");
        var start = Expression.Property(r, nameof(Tidsrymd.Start));
        var slut  = Expression.Property(r, nameof(Tidsrymd.Slut));
        var body = Expression.AndAlso(Expression.GreaterThanOrEqual(t, start), Expression.LessThan(t, slut));
        return Expression.Lambda(body, r, t);
    }
    
    [ExpansionFor(typeof(Tidsrymd), nameof(Tidsrymd.Överlappar))]
    public static LambdaExpression Överlappar_Expansion()
    {
        var a = Expression.Parameter(typeof(Tidsrymd), "a");
        var b = Expression.Parameter(typeof(Tidsrymd), "b");
        var aStart = Expression.Property(a, nameof(Tidsrymd.Start));
        var aSlut  = Expression.Property(a, nameof(Tidsrymd.Slut));
        var bStart = Expression.Property(b, nameof(Tidsrymd.Start));
        var bSlut  = Expression.Property(b, nameof(Tidsrymd.Slut));
        var body = Expression.AndAlso(Expression.LessThan(aStart, bSlut), Expression.LessThan(bStart, aSlut));
        return Expression.Lambda(body, a, b);
    }
}
