using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using VGR.Semantics.Abstractions;

namespace VGR.Semantics.Linq;

/// <summary>
/// Central registry of domain-method → EF-friendly expression rewrites.
/// Discovers [ExpansionFor] methods at runtime via reflection.
/// The source generator (SemanticGenerator) provides compile-time validation
/// but the actual wiring is dynamic — expansions can live in any loaded assembly.
/// </summary>
internal static class SemanticRegistry
{
    private static readonly ConcurrentDictionary<MethodInfo, LambdaExpression> _registry = new();

    static SemanticRegistry()
    {
        DiscoverExpansions();
    }

    private static void DiscoverExpansions()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip framework assemblies
            var name = assembly.GetName().Name;
            if (name is null || name.StartsWith("System") || name.StartsWith("Microsoft") ||
                name.StartsWith("netstandard") || name.StartsWith("mscorlib"))
                continue;

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t is not null).ToArray()!; }

            foreach (var type in types)
            {
                if (!type.IsAbstract || !type.IsSealed) continue; // static classes only

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    foreach (var attr in method.GetCustomAttributes<ExpansionForAttribute>())
                    {
                        var lambda = method.Invoke(null, null) as LambdaExpression;
                        if (lambda is null) continue;

                        var targetMember = attr.TargetType
                            .GetMember(attr.TargetMethodName, BindingFlags.Public | BindingFlags.Instance)
                            .FirstOrDefault();

                        MethodInfo? key = targetMember switch
                        {
                            MethodInfo mi => mi,
                            PropertyInfo pi => pi.GetGetMethod(true),
                            _ => null
                        };

                        if (key is not null)
                            _registry[key] = lambda;
                    }
                }
            }
        }
    }

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
    
    internal static Expression Rewrite(Expression expr)
    {
        var rewriter = new Rewriter(_registry);
        var current = expr;

        // Expand until stable — handles nested expansions like ÄrAktivt → ÄrTillsvidare → Slut == null
        for (var i = 0; i < 8; i++)
        {
            var next = rewriter.Visit(current)!;
            if (ReferenceEquals(next, current)) break;
            current = next;
        }

        return current;
    }

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
