using System.Linq.Expressions;
using System.Reflection;

namespace VGR.Semantics.Linq;

/// <summary>
/// Central registry of domain-method → EF-friendly expression rewrites.
/// </summary>
internal static partial class SemanticRegistry
{
    private static readonly Dictionary<MethodInfo, LambdaExpression> _registry = [];

    public static bool TryGet(MethodInfo m, out LambdaExpression lambda)
        => _registry.TryGetValue(m, out lambda!);
    

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
        switch (domainCall.Body)
        {
            case MethodCallExpression m:
                Register(m.Method, efExpression);
                break;
            case MemberExpression me when me.Member is PropertyInfo pi:
                var getter = pi.GetGetMethod(true) ?? throw new InvalidOperationException("Property has no getter.");
                Register(getter, efExpression);
                break;
            default:
                throw new InvalidOperationException("domainCall must be a MethodCallExpression or a property access.");
        }
    }

    private static void Register(MethodInfo domainMethod, LambdaExpression efExpression)
    {
        if (domainMethod is null) throw new ArgumentNullException(nameof(domainMethod));
        if (efExpression is null) throw new ArgumentNullException(nameof(efExpression));
        _registry[domainMethod] = efExpression;
    }
    
    internal static Expression Rewrite(Expression expr) => new Rewriter(_registry).Visit(expr)!;

    private sealed class Rewriter(IReadOnlyDictionary<MethodInfo, LambdaExpression> map) : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<MethodInfo, LambdaExpression> _map = map;

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

        protected override Expression VisitMember(MemberExpression node)
        {
            var visited = (MemberExpression)base.VisitMember(node);

            if (visited.Member is PropertyInfo pi)
            {
                var getter = pi.GetGetMethod(true);
                if (getter is not null && _map.TryGetValue(getter, out var replacement))
                {
                    var args = new List<Expression>(visited.Expression is null ? 0 : 1);
                    if (visited.Expression is not null) args.Add(visited.Expression);

                    if (replacement.Parameters.Count != args.Count)
                        throw new InvalidOperationException("Replacement parameter count mismatch");

                    var subst = new ParameterSubstituter(replacement.Parameters, args);
                    return subst.Visit(replacement.Body);
                }
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
