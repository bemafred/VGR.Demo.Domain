using System.Linq.Expressions;
using System.Reflection;

namespace VGR.Semantics.Queries;

/// <summary>
/// Central registry of domain-method → EF-friendly expression rewrites.
/// </summary>
internal static partial class SemanticRegistry
{
    private static readonly Dictionary<MethodInfo, LambdaExpression> _registry = [];

    public static void Register(MethodInfo domainMethod, LambdaExpression efExpression)
    {
        if (domainMethod is null) throw new ArgumentNullException(nameof(domainMethod));
        if (efExpression is null) throw new ArgumentNullException(nameof(efExpression));
        _registry[domainMethod] = efExpression;
    }

    public static void Register<T1, T2, TResult>(
        Expression<Func<T1, T2, TResult>> domainCall,
        Expression<Func<T1, T2, TResult>> efExpression)
    {
        if (domainCall.Body is not MethodCallExpression m)
            throw new InvalidOperationException("domainCall must be MethodCallExpression");

        Register(m.Method, efExpression);
    }

    public static void Register<T1, TResult>(
        Expression<Func<T1, TResult>> domainCall,
        Expression<Func<T1, TResult>> efExpression)
    {
        if (domainCall.Body is not MethodCallExpression m)
            throw new InvalidOperationException("domainCall must be MethodCallExpression");

        Register(m.Method, efExpression);
    }

    internal static Expression Rewrite(Expression expr) => new Rewriter(_registry).Visit(expr)!;

    private sealed class Rewriter : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<MethodInfo, LambdaExpression> _map;
        public Rewriter(IReadOnlyDictionary<MethodInfo, LambdaExpression> map) => _map = map;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var visited = (MethodCallExpression)base.VisitMethodCall(node);

            if (_map.TryGetValue(visited.Method, out var replacement))
            {
                var args = new List<Expression>(visited.Arguments.Count + (visited.Object is null ? 0 : 1));
                if (visited.Object is not null) args.Add(visited.Object);
                args.AddRange(visited.Arguments);

                if (replacement.Parameters.Count != args.Count)
                    throw new InvalidOperationException("Replacement parameter count mismatch");

                var subst = new ParameterSubstituter(replacement.Parameters, args);
                return subst.Visit(replacement.Body);
            }

            return visited;
        }

        private sealed class ParameterSubstituter : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<ParameterExpression, Expression> _map;
            public ParameterSubstituter(IReadOnlyList<ParameterExpression> ps, IReadOnlyList<Expression> args)
            {
                var d = new Dictionary<ParameterExpression, Expression>(ps.Count);
                for (int i = 0; i < ps.Count; i++) d[ps[i]] = args[i];
                _map = d;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => _map.TryGetValue(node, out var repl) ? repl : node;
        }
    }
}
