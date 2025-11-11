using System.Linq;
namespace VGR.Semantics;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> to support semantic query-rewriting functionality.
/// This class enables users to apply custom semantic transformations to LINQ queries.
/// </summary>
/// <remarks>
/// The functionality uses an internal mechanism to traverse and rewrite expressions within a query,
/// leveraging attributes and a semantic registry for method expansion.
/// </remarks>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies semantic transformations to the given <see cref="IQueryable{T}"/> based on custom query semantics defined within the system.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source IQueryable.</typeparam>
    /// <param name="q">The source IQueryable to which the semantic transformations are applied.</param>
    /// <returns>A new <see cref="IQueryable{T}"/> instance with the applied semantic transformations.</returns>
    public static IQueryable<T> WithSemantics<T>(this IQueryable<T> q)
    {
        var r = new QuerySemanticRewriter();
        var e = r.Visit(q.Expression);
        return q.Provider.CreateQuery<T>(e);
    }
}
