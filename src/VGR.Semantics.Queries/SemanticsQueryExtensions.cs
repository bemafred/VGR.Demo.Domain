using System.Linq.Expressions;

namespace VGR.Semantics.Queries;

public static class SemanticQueryExtensions
{
    public static IQueryable<T> WithSemantics<T>(this IQueryable<T> source, Semantic semantic)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (semantic is null) throw new ArgumentNullException(nameof(semantic));
        return new SemanticQueryable<T>(source, semantic);
    }

    private sealed class SemanticQueryable<T> : IQueryable<T>, IQueryProvider
    {
        private readonly IQueryable<T> _inner;
        private readonly IQueryProvider _provider;

        public SemanticQueryable(IQueryable<T> inner, Semantic semantic)
        {
            _inner = inner;
            _provider = new SemanticQueryProvider(inner.Provider, semantic);
        }

        public Type ElementType => typeof(T);
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => this;

        public IEnumerator<T> GetEnumerator() => _provider.CreateQuery<T>(Expression).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        IQueryable IQueryProvider.CreateQuery(Expression expression) => _provider.CreateQuery(expression);
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => _provider.CreateQuery<TElement>(expression);
        object? IQueryProvider.Execute(Expression expression) => _provider.Execute(expression);
        TResult IQueryProvider.Execute<TResult>(Expression expression) => _provider.Execute<TResult>(expression);
    }
}
