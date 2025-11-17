using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using VGR.Semantics.Abstractions;

namespace VGR.Semantics.Queries;

/// <summary>
/// The <see cref="SemanticQueryRewriter"/> class rewrites LINQ expression trees by applying transformations based on
/// custom query semantics. It identifies methods annotated with the <see cref="SemanticQueryAttribute"/> and replaces
/// them with semantically equivalent expressions as defined within the system.
/// </summary>
/// <remarks>
/// This class is an internal implementation detail and should not be used directly by consumers of the API.
/// It extends <see cref="ExpressionVisitor"/> to traverse and transform expression trees.
/// </remarks>
internal sealed class SemanticQueryRewriter : ExpressionVisitor
{
    /// <summary>
    /// Visits a <see cref="MethodCallExpression"/> within an expression tree and rewrites it
    /// if the associated method has a <see cref="QuerySemanticAttribute"/>.
    /// </summary>
    /// <param name="node">The <see cref="MethodCallExpression"/> that is being visited and potentially rewritten.</param>
    /// <returns>
    /// The transformed <see cref="Expression"/> if the method is annotated with <see cref="QuerySemanticAttribute"/>
    /// and a matching semantic is found; otherwise, the original or a base-visited expression.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no semantic transformation exists in the registry for the method being analyzed.
    /// </exception>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var m = node.Method;
        if (m.GetCustomAttribute<SemanticQueryAttribute>() is null) return base.VisitMethodCall(node);
        if (!SemanticRegistry.TryGet(m, out var lambda))
            throw new InvalidOperationException($"No expansion for {m.DeclaringType?.Name}.{m.Name}");
        var args = new System.Collections.Generic.List<Expression>();
        if (node.Object is not null) args.Add(node.Object);
        args.AddRange(node.Arguments);
        var body = new Sub(lambda.Parameters, args).Visit(lambda.Body);
        return Visit(body);
    }

    /// <summary>
    /// Visits a <see cref="MemberExpression"/> within an expression tree and rewrites it
    /// if the associated member (property) has a <see cref="SemanticQueryAttribute"/>.
    /// </summary>
    /// <param name="node">The <see cref="MemberExpression"/> representing a property access in an expression tree.</param>
    /// <returns>
    /// The transformed <see cref="Expression"/> if the property is annotated with <see cref="SemanticQueeryAttribute"/>
    /// and a corresponding semantic transformation exists; otherwise, the original or a base-visited expression.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no semantic transformation exists in the registry for the accessed property.
    /// </exception>
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is PropertyInfo pi && pi.GetCustomAttribute<SemanticQueryAttribute>() is not null)
        {
            var getter = pi.GetMethod;
            if (getter is not null && SemanticRegistry.TryGet(getter, out var lambda))
            {
                var args = new List<Expression>();
                if (node.Expression is not null) args.Add(node.Expression);
                var body = new Sub(lambda.Parameters, args).Visit(lambda.Body);
                return Visit(body);
            }
        }
        return base.VisitMember(node);
    }    
    
    /// <summary>
    /// The <see cref="Sub"/> class is responsible for substituting parameter expressions with their corresponding
    /// replacements within an expression tree. It creates a mapping of parameter expressions to their replacement
    /// expressions during construction and applies this mapping when visiting nodes in the tree.
    /// </summary>
    /// <remarks>
    /// This class is an internal helper designed to work within the context of the query semantics rewriting process.
    /// It extends <see cref="ExpressionVisitor"/> to traverse and transform parameter expressions as needed.
    /// </remarks>
    private sealed class Sub : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, Expression> _map = new();
        public Sub(System.Collections.Generic.IReadOnlyList<ParameterExpression> ps, System.Collections.Generic.IReadOnlyList<Expression> xs)
        { for (int i=0;i<ps.Count;i++) _map[ps[i]] = xs[i]; }
        protected override Expression VisitParameter(ParameterExpression node) => _map.TryGetValue(node, out var v) ? v : node;
    }
}

