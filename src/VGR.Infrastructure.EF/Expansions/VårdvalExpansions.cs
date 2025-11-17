using System;
using System.Linq.Expressions;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Abstractions;

namespace VGR.Infrastructure.EF.Expansions;

/// <summary>
/// Provides extension methods for defining and utilizing custom expansions on the <see cref="Vårdval"/> class,
/// specifically for use in expression trees and query processing.
/// </summary>
public static class VårdvalExpansions
{
    /// <summary>
    /// Creates a lambda expression that determines whether a <see cref="Vårdval"/> instance is active
    /// during a specified time period. The method evaluates if the provided time falls within the
    /// start and end of the <see cref="Tidsrymd"/> associated with the <see cref="Vårdval"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="LambdaExpression"/> that represents the activation status of a <see cref="Vårdval"/>
    /// based on its time period.
    /// </returns>
    [ExpansionFor(typeof(Vårdval), nameof(Vårdval.ÄrAktivt))]
    public static Expression<Func<Vårdval, bool>> ÄrAktivt_Expansion()
        => v => v.Period.Slut == null;
}
