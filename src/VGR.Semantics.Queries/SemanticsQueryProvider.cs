using System.Linq.Expressions;

namespace VGR.Semantics.Queries;

internal sealed class SemanticQueryProvider(IQueryProvider inner) : IQueryProvider
{
    private readonly IQueryProvider _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public IQueryable CreateQuery(Expression expression)
        => _inner.CreateQuery(SemanticRegistry.Rewrite(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => _inner.CreateQuery<TElement>(SemanticRegistry.Rewrite(expression));

    public object? Execute(Expression expression)
        => _inner.Execute(SemanticRegistry.Rewrite(expression));

    public TResult Execute<TResult>(Expression expression)
        => _inner.Execute<TResult>(SemanticRegistry.Rewrite(expression));
}
