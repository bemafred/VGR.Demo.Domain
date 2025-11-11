using System;
namespace VGR.Semantics;
[AttributeUsage(AttributeTargets.Method)] public sealed class QuerySemanticAttribute : Attribute {}
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)] public sealed class ExpansionForAttribute : Attribute
{ public Type TargetType {get;} public string TargetMethodName {get;} public ExpansionForAttribute(Type t, string name){TargetType=t;TargetMethodName=name;} }
