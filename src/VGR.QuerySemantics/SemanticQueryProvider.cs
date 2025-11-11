using System.Linq.Expressions;

namespace VGR.QuerySemantics;

internal sealed class SemanticQueryProvider : IQueryProvider
{
    private readonly IQueryProvider _inner;
    private readonly QuerySemantics _semantics;

    public SemanticQueryProvider(IQueryProvider inner, QuerySemantics semantics)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _semantics = semantics ?? throw new ArgumentNullException(nameof(semantics));
    }

    public IQueryable CreateQuery(Expression expression)
        => _inner.CreateQuery(_semantics.Rewrite(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => _inner.CreateQuery<TElement>(_semantics.Rewrite(expression));

    public object? Execute(Expression expression)
        => _inner.Execute(_semantics.Rewrite(expression));

    public TResult Execute<TResult>(Expression expression)
        => _inner.Execute<TResult>(_semantics.Rewrite(expression));
}
