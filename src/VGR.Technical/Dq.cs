using System.Linq.Expressions;

namespace VGR.Technical;

public sealed record class Dq<T>(Expression<Func<T, bool>> Predicate)
{
    public Dq<T> And(Expression<Func<T,bool>> other) => new(Predicate.And(other));
    public Dq<T> Or(Expression<Func<T,bool>> other) => new(Predicate.Or(other));
}

internal static class ExprCombos
{
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
        => a.Compose(b, Expression.AndAlso);

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
        => a.Compose(b, Expression.OrElse);

    private static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second,
        Func<Expression, Expression, Expression> merge)
    {
        var map = first.Parameters.Select((f,i) => new { f, s = second.Parameters[i]})
                                  .ToDictionary(p => p.s, p => p.f);
        var secondBody = new Replace(map).Visit(second.Body)!;
        return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
    }

    private sealed class Replace(IDictionary<ParameterExpression, ParameterExpression> map) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => map.TryGetValue(node, out var r) ? r : node;
    }
}