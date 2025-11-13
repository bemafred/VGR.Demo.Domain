using System;
namespace VGR.Semantics.Abstractions;

/// <summary>
/// Represents an attribute used to denote that a method has a semantic meaning in query-related contexts.
/// This attribute is applied to methods to indicate they participate in custom query processing or rewriting.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticQueryableAttribute : Attribute { }

/// <summary>
/// Represents an attribute used to specify an expansion method for a target type and method.
/// This attribute is applied to methods that provide custom expression tree representations
/// for specific methods of a target type, enabling enhanced query processing or alternate behavior during execution.
/// </summary>
/// <param name="t">The target type whose method is being expanded.</param>
/// <param name="name">The name of the target method for which this expansion applies.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ExpansionForAttribute(Type t, string name) : Attribute
{
    public Type TargetType {get;} = t;
    public string TargetMethodName {get;} = name;
}
