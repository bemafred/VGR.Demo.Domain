using System.Linq;
namespace VGR.Semantics;
public static class QueryableExtensions
{
    public static IQueryable<T> WithSemantics<T>(this IQueryable<T> q)
    {
        var r = new QuerySemanticRewriter();
        var e = r.Visit(q.Expression);
        return q.Provider.CreateQuery<T>(e);
    }
}
