using System.Linq.Expressions;

namespace VGR.QuerySemantics;

public static class QueryableSemanticsExtensions
{
    public static IQueryable<T> WithSemantics<T>(this IQueryable<T> source, QuerySemantics semantics)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (semantics is null) throw new ArgumentNullException(nameof(semantics));
        return new SemanticQueryable<T>(source, semantics);
    }

    private sealed class SemanticQueryable<T> : IQueryable<T>, IQueryProvider
    {
        private readonly IQueryable<T> _inner;
        private readonly IQueryProvider _provider;

        public SemanticQueryable(IQueryable<T> inner, QuerySemantics semantics)
        {
            _inner = inner;
            _provider = new SemanticQueryProvider(inner.Provider, semantics);
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
