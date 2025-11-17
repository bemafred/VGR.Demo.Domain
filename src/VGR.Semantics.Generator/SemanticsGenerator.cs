using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Text;

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

        var expansionForAttrName = "VGR.Semantics.Abstractions.ExpansionForAttribute";
        var expansionAttr = comp.GetTypeByMetadataName(expansionForAttrName);

        var semanticQueryAttrName = "VGR.Semantics.Abstractions.SemanticQueryAttribute";
        var semanticAttr = comp.GetTypeByMetadataName(semanticQueryAttrName); 

        if (expansionAttr is null || semanticAttr is null) return;

        var pairs = new System.Collections.Generic.List<(IMethodSymbol Target, IMethodSymbol Factory)>();
        
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
                    .Where(m => IsSemanticMember(m, semanticAttr))
                    .ToArray();
                
                if (candidates.Length == 0) 
                    continue;
                
                var target = ExtractTargetMethod(candidates[0]);
                if (target is null)
                    continue;

                pairs.Add((target, factory));
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("namespace VGR.Semantics.Queries;");
        sb.AppendLine("internal static partial class SemanticRegistry");
        sb.AppendLine("{");
        sb.AppendLine("  static SemanticRegistry()");
        sb.AppendLine("  {");

        foreach (var p in pairs)
        {
            var tType = p.Target.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
            var fType = p.Factory.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");

            var binding = p.Target.IsStatic
                ? "BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static"
                : "BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance";

            var paramTypes = BuildParameterTypesList(p.Target);

            sb.AppendLine("    {");
            sb.AppendLine($"      var mi = typeof({tType}).GetMethod(");
            sb.AppendLine($"        name: \"{p.Target.Name}\",");
            sb.AppendLine($"        bindingAttr: {binding},");
            sb.AppendLine("        binder: null,");
            sb.AppendLine($"        types: {paramTypes},");
            sb.AppendLine("        modifiers: null);");
            sb.AppendLine("      if (mi is null) throw new InvalidOperationException(\"SemanticRegistry generator: target method not found.\");");

            if (p.Target.IsGenericMethod && SymbolEqualityComparer.Default.Equals(p.Target.OriginalDefinition, p.Target))
            {
                var typeArgs = p.Target.TypeParameters.Length == 0
                    ? ""
                    : string.Join(", ", p.Target.TypeParameters.Select(tp => "typeof(object)"));
                if (typeArgs.Length > 0)
                {
                    sb.AppendLine("      // NOTE: Generic method placeholder closing with object; adjust if generic expansions are introduced.");
                    sb.AppendLine("      if (mi.IsGenericMethodDefinition) mi = mi.MakeGenericMethod(new[]{ " + typeArgs + " });");
                }
            }

            sb.AppendLine($"      _registry[mi] = {fType}.{p.Factory.Name}();");
            sb.AppendLine("    }");
        }

        sb.AppendLine("  }");
        sb.AppendLine("}");

        context.AddSource("SemanticRegistry.g.cs", sb.ToString());
    }

    /// <summary>Kontrollerar om en medlem är annoterad med SemanticQueryAttribute.</summary>
    private static bool IsSemanticMember(ISymbol member, INamedTypeSymbol semanticAttr) =>
        member switch
        {
            IMethodSymbol method => method.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, semanticAttr)),
            
            IPropertySymbol property => property.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, semanticAttr)),
            
            _ => false
        };

    /// <summary>Extraherar målmetoden från en medlem (metod eller property getter).</summary>
    private static IMethodSymbol? ExtractTargetMethod(ISymbol candidate) =>
        candidate switch
        {
            IMethodSymbol method => method,
            IPropertySymbol property => property.GetMethod,
            _ => null
        };

    /// <summary>Bygger parametertypslistan för reflection-anropet.</summary>
    private static string BuildParameterTypesList(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
            return "Array.Empty<Type>()";

        var types = method.Parameters.Select(par =>
            "typeof(" + par.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "") + ")");
        
        return "new Type[]{ " + string.Join(", ", types) + " }";
    }

    private sealed class Receiver : ISyntaxReceiver
    {
        public System.Collections.Generic.List<MethodDeclarationSyntax> Candidates { get; } = [];
        
        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0)
                Candidates.Add(m);
        }
    }
}
