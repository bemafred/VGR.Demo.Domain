using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Text;

using VGR.Semantics.Abstractions;

namespace VGR.Semantics.Generator;

[Generator]
public sealed class SemanticGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(() => new Receiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not Receiver rx) return;
        
        var comp = context.Compilation;
        var expansionForAttrName = typeof(ExpansionForAttribute).FullName!;
        var expansionAttr = comp.GetTypeByMetadataName(expansionForAttrName);
        var semanticQueryAttrName = typeof(SemanticQueryAttribute).FullName!;
        var semanticAttr = comp.GetTypeByMetadataName(semanticQueryAttrName); 
        if (expansionAttr is null || semanticAttr is null) return;

        var pairs = new System.Collections.Generic.List<(IMethodSymbol Target, IMethodSymbol Factory, int Arity)>();
        
        foreach (var methodDecl in rx.Candidates)
        {
            var model = comp.GetSemanticModel(methodDecl.SyntaxTree);
            if (model.GetDeclaredSymbol(methodDecl) is not IMethodSymbol factory) continue;
            foreach (var attr in factory.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, expansionAttr)) continue;
                if (attr.ConstructorArguments.Length != 2) continue;
                var targetType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
                var targetName = attr.ConstructorArguments[1].Value as string;
                if (targetType is null || targetName is null) continue;

                var candidates = targetType.GetMembers(targetName)
                    .Where(m =>
                        (m is IMethodSymbol mm && mm.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, semanticAttr))) ||
                        (m is IPropertySymbol pp && pp.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, semanticAttr)))
                    )
                    .ToArray();
                
                if (candidates.Length == 0) 
                    continue;
                
                IMethodSymbol target;
                int arity;

                if (candidates[0] is IMethodSymbol mm)
                {
                    target = mm;
                    arity  = (target.IsStatic ? 0 : 1) + target.Parameters.Length;
                }
                else if (candidates[0] is IPropertySymbol pp && pp.GetMethod is not null)
                {
                    target = pp.GetMethod; // property ⇒ get_*
                    arity  = (target.IsStatic ? 0 : 1) + target.Parameters.Length; // oftast 1
                }
                else
                    continue;

                pairs.Add((target, factory, arity));            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("namespace VGR.Semantics.Queries;");
        sb.AppendLine("internal static partial class SemanticRegistry");
        sb.AppendLine("{");
        sb.AppendLine("  private static readonly System.Collections.Generic.Dictionary<(MethodInfo,int), LambdaExpression> __map = new();");
        sb.AppendLine("  static SemanticRegistry()");
        sb.AppendLine("  {");

        foreach (var p in pairs)
        {
            var tType = p.Target.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::","");
            var binding = p.Target.IsStatic ? "BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static" : "BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance";
            var fType = p.Factory.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::","");

            sb.AppendLine("{");
            sb.AppendLine($"    var t = Type.GetType(\"{tType}\");");
            sb.AppendLine("    var mi = t.GetMethods(" + binding + ").First(m => m.Name == \"" + p.Target.Name + "\" && m.GetParameters().Length == " + p.Target.Parameters.Length + " && m.IsStatic == " + (p.Target.IsStatic ? "true" : "false") + ");");
            sb.AppendLine($"    __map[(mi, {p.Arity})] = {fType}.{p.Factory.Name}();");
            sb.AppendLine("}");
        }

        sb.AppendLine("  }");
        sb.AppendLine("  public static bool TryGet(MethodInfo method, int arity, out LambdaExpression lambda) => __map.TryGetValue((method,arity), out lambda!);");
        sb.AppendLine("}");
        context.AddSource("SemanticRegistry.g.cs", sb.ToString());
    }

    private sealed class Receiver : ISyntaxReceiver
    {
        public System.Collections.Generic.List<MethodDeclarationSyntax> Candidates { get; } = new();
        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0)
                Candidates.Add(m);
        }
    }
}
