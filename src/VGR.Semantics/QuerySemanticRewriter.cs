using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
namespace VGR.Semantics;
internal sealed class QuerySemanticRewriter : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var m = node.Method;
        if (m.GetCustomAttribute<QuerySemanticAttribute>() is null) return base.VisitMethodCall(node);
        if (!SemanticRegistry.TryGet(m, (node.Object is null ? 0 : 1) + node.Arguments.Count, out var lambda))
            throw new InvalidOperationException($"No expansion for {m.DeclaringType?.Name}.{m.Name}");
        var args = new System.Collections.Generic.List<Expression>();
        if (node.Object is not null) args.Add(node.Object);
        args.AddRange(node.Arguments);
        var body = new Sub(lambda.Parameters, args).Visit(lambda.Body);
        return Visit(body);
    }
    private sealed class Sub : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, Expression> _map = new();
        public Sub(System.Collections.Generic.IReadOnlyList<ParameterExpression> ps, System.Collections.Generic.IReadOnlyList<Expression> xs)
        { for (int i=0;i<ps.Count;i++) _map[ps[i]] = xs[i]; }
        protected override Expression VisitParameter(ParameterExpression node) => _map.TryGetValue(node, out var v) ? v : node;
    }
}
internal static partial class SemanticRegistry
{
    public static bool TryGet(MethodInfo m, int arity, out LambdaExpression lambda) { lambda = null!; return false; }
}
