using System.Linq.Expressions;

namespace VGR.Semantics.Queries;

internal sealed class SemanticQueryProvider(IQueryProvider inner, Semantic semantic) : IQueryProvider
{
    private readonly IQueryProvider _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Semantic _semantic = semantic ?? throw new ArgumentNullException(nameof(semantic));

    public IQueryable CreateQuery(Expression expression)
        => _inner.CreateQuery(_semantic.Rewrite(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => _inner.CreateQuery<TElement>(_semantic.Rewrite(expression));

    public object? Execute(Expression expression)
        => _inner.Execute(_semantic.Rewrite(expression));

    public TResult Execute<TResult>(Expression expression)
        => _inner.Execute<TResult>(_semantic.Rewrite(expression));
}
