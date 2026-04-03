using System.Linq.Expressions;

namespace VGR.Semantics.Linq;

/// <summary>Extensionmetoder för att aktivera semantisk omskrivning av LINQ-frågor.</summary>
public static class SemanticQueryExtensions
{
    /// <summary>Wrapprar en <see cref="IQueryable{T}"/> så att domänmetoder (t.ex. <c>Innehåller</c>, <c>Överlappar</c>) automatiskt skrivs om till EF-kompatibla uttryck.</summary>
    public static IQueryable<T> WithSemantics<T>(this IQueryable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return new SemanticQueryable<T>(source);
    }

    private sealed class SemanticQueryable<T> : IQueryable<T>, IQueryProvider
    {
        private readonly IQueryable<T> _inner;
        private readonly IQueryProvider _provider;

        public SemanticQueryable(IQueryable<T> inner)
        {
            _inner = inner;
            _provider = new SemanticQueryProvider(inner.Provider);
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
