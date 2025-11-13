using System.Linq.Expressions;

namespace VGR.Semantics.Queries;

internal sealed class SemanticQueryProvider(IQueryProvider inner, SemanticMappings semanticMappings) : IQueryProvider
{
    private readonly IQueryProvider _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly SemanticMappings _semanticMappings = semanticMappings ?? throw new ArgumentNullException(nameof(semanticMappings));

    public IQueryable CreateQuery(Expression expression)
        => _inner.CreateQuery(_semanticMappings.Rewrite(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => _inner.CreateQuery<TElement>(_semanticMappings.Rewrite(expression));

    public object? Execute(Expression expression)
        => _inner.Execute(_semanticMappings.Rewrite(expression));

    public TResult Execute<TResult>(Expression expression)
        => _inner.Execute<TResult>(_semanticMappings.Rewrite(expression));
}
