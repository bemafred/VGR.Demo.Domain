using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace VGR.Semantics.Generator;

[Generator]
public sealed class SemanticGenerator : IIncrementalGenerator
{
    // Diagnostiska beskrivningar
    private static readonly DiagnosticDescriptor MissingExpansionRule = new(
        id: "SEMANTICS001",
        title: "Saknad expansion för semantisk query",
        messageFormat: "Metoden/propertyn '{0}.{1}' är markerad med [SemanticQuery] men saknar motsvarande [ExpansionFor]-metod",
        category: "Semantics",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Varje [SemanticQuery] måste ha en matchande [ExpansionFor] i Infrastructure.EF/Expansions.");

    private static readonly DiagnosticDescriptor OrphanedExpansionRule = new(
        id: "SEMANTICS002",
        title: "Orphaned expansion utan semantisk query",
        messageFormat: "Expansionsmetoden '{0}' pekar på '{1}.{2}' som inte är markerad med [SemanticQuery]",
        category: "Semantics",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "En [ExpansionFor] bör endast definieras för medlemmar markerade med [SemanticQuery].");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Samla alla metoder med [ExpansionFor]
        var expansionMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => (MethodDeclarationSyntax)ctx.Node)
            .Where(static m => m is not null);

        // Samla alla metoder/properties med potentiell [SemanticQuery]
        var semanticMembers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MemberDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => ctx.Node)
            .Where(static m => m is not null);

        var compilation = context.CompilationProvider;
        var combined = compilation
            .Combine(expansionMethods.Collect())
            .Combine(semanticMembers.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) =>
            Execute(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<MethodDeclarationSyntax> expansionMethodDeclarations,
        ImmutableArray<SyntaxNode> semanticMemberDeclarations)
    {
        var expansionForAttrName = "VGR.Semantics.Abstractions.ExpansionForAttribute";
        var expansionAttr = compilation.GetTypeByMetadataName(expansionForAttrName);

        var semanticQueryAttrName = "VGR.Semantics.Abstractions.SemanticQueryAttribute";
        var semanticAttr = compilation.GetTypeByMetadataName(semanticQueryAttrName);

        if (expansionAttr is null || semanticAttr is null) return;

        // Bygg register över alla [SemanticQuery]-medlemmar
        var semanticMembersDict = new Dictionary<string, (ISymbol Symbol, Location Location)>();

        foreach (var memberDecl in semanticMemberDeclarations)
        {
            var model = compilation.GetSemanticModel(memberDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(memberDecl);

            if (symbol is null) continue;

            if (symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, semanticAttr)))
            {
                var key = GetMemberKey(symbol);
                semanticMembersDict[key] = (symbol, symbol.Locations.FirstOrDefault() ?? Location.None);
            }
        }

        // Bygg register över alla [ExpansionFor]-mappningar
        var expansionPairs = new List<(ISymbol Target, IMethodSymbol Factory)>();
        var mappedSemanticMembers = new HashSet<string>();

        foreach (var methodDecl in expansionMethodDeclarations)
        {
            var model = compilation.GetSemanticModel(methodDecl.SyntaxTree);
            if (model.GetDeclaredSymbol(methodDecl) is not IMethodSymbol factory) continue;

            foreach (var attr in factory.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, expansionAttr)) continue;
                if (attr.ConstructorArguments.Length != 2) continue;

                var targetType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
                var targetName = attr.ConstructorArguments[1].Value as string;
                if (targetType is null || targetName is null) continue;

                var candidates = targetType.GetMembers(targetName).ToArray();

                if (candidates.Length == 0)
                {
                    // Target medlem finns inte alls
                    var attrLocation = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? methodDecl.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        OrphanedExpansionRule,
                        attrLocation,
                        factory.Name,
                        targetType.Name,
                        targetName));
                    continue;
                }

                var semanticCandidate = candidates.FirstOrDefault(c => IsSemanticMember(c, semanticAttr));

                if (semanticCandidate is null)
                {
                    // Expansion pekar på medlem utan [SemanticQuery]
                    var attrLocation = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? methodDecl.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        OrphanedExpansionRule,
                        attrLocation,
                        factory.Name,
                        targetType.Name,
                        targetName));
                    continue;
                }

                expansionPairs.Add((semanticCandidate, factory));
                mappedSemanticMembers.Add(GetMemberKey(semanticCandidate));
            }
        }

        // Rapportera saknade expansions för [SemanticQuery]-medlemmar
        foreach (var (key, (symbol, location)) in semanticMembersDict)
        {
            if (!mappedSemanticMembers.Contains(key))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingExpansionRule,
                    location,
                    symbol.ContainingType.Name,
                    symbol.Name));
            }
        }

        // Generera registret (som tidigare)
        GenerateRegistry(context, expansionPairs);
    }

    private static string GetMemberKey(ISymbol symbol)
    {
        var typeName = symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{typeName}::{symbol.Name}";
    }

    private static void GenerateRegistry(
        SourceProductionContext context,
        List<(ISymbol Target, IMethodSymbol Factory)> pairs)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("using System.Diagnostics.CodeAnalysis;");
        sb.AppendLine();
        sb.AppendLine("namespace VGR.Semantics.Linq;");
        sb.AppendLine();
        sb.AppendLine("internal static partial class SemanticRegistry");
        sb.AppendLine("{");

        // AOT-säker MethodInfo cache
        sb.AppendLine("  /// <summary>AOT-safe MethodInfo cache via expression extraction.</summary>");
        sb.AppendLine("  private static class MethodCache");
        sb.AppendLine("  {");

        int cacheIndex = 0;
        var cacheNames = new List<string>();

        foreach (var (target, factory) in pairs)
        {
            var cacheName = $"Method_{cacheIndex++}";
            cacheNames.Add(cacheName);

            var containingType = target switch
            {
                IMethodSymbol m => m.ContainingType,
                IPropertySymbol p => p.ContainingType,
                _ => throw new InvalidOperationException("Unexpected symbol type")
            };

            var tType = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");

            sb.AppendLine($"    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof({tType}))]");

            switch (target)
            {
                case IPropertySymbol prop:
                    GeneratePropertyCache(sb, cacheName, tType, prop);
                    break;

                case IMethodSymbol method:
                    GenerateMethodCache(sb, cacheName, tType, method);
                    break;
            }

            sb.AppendLine();
        }

        sb.AppendLine("  }");
        sb.AppendLine();

        // Statisk konstruktor - registrering
        sb.AppendLine("  static SemanticRegistry()");
        sb.AppendLine("  {");

        for (int i = 0; i < pairs.Count; i++)
        {
            var (_, factory) = pairs[i];
            var fType = factory.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");

            sb.AppendLine($"    _registry[MethodCache.{cacheNames[i]}] = {fType}.{factory.Name}();");
        }

        sb.AppendLine("  }");
        sb.AppendLine("}");

        context.AddSource("SemanticRegistry.g.cs", sb.ToString());
    }

    private static void GeneratePropertyCache(StringBuilder sb, string cacheName, string typeName, IPropertySymbol property)
    {
        var returnType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");

        sb.AppendLine($"    internal static readonly MethodInfo {cacheName} =");
        sb.AppendLine($"      ((PropertyInfo)((MemberExpression)");
        sb.AppendLine($"        ((Expression<Func<{typeName}, {returnType}>>)(x => x.{property.Name})).Body)");
        sb.AppendLine($"        .Member).GetGetMethod(nonPublic: true)!;");
    }

    private static void GenerateMethodCache(StringBuilder sb, string cacheName, string typeName, IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            // Parameterless method - använd Action
            sb.AppendLine($"    internal static readonly MethodInfo {cacheName} =");
            sb.AppendLine($"      ((MethodCallExpression)");
            sb.AppendLine($"        ((Expression<Action<{typeName}>>)(x => x.{method.Name}())).Body)");
            sb.AppendLine($"        .Method;");
        }
        else
        {
            // Method med parametrar
            var paramTypes = method.Parameters
                .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", ""))
                .ToList();

            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");

            var paramNames = string.Join(", ", Enumerable.Range(0, method.Parameters.Length).Select(i => $"p{i}"));
            var allTypes = string.Join(", ", new[] { typeName }.Concat(paramTypes).Append(returnType));

            sb.AppendLine($"    internal static readonly MethodInfo {cacheName} =");
            sb.AppendLine($"      ((MethodCallExpression)");
            sb.AppendLine($"        ((Expression<Func<{allTypes}>>)");
            sb.AppendLine($"          ((x, {paramNames}) => x.{method.Name}({paramNames}))).Body)");
            sb.AppendLine($"        .Method;");
        }
    }

    private static bool IsSemanticMember(ISymbol member, INamedTypeSymbol semanticAttr) =>
        member switch
        {
            IMethodSymbol method => method.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, semanticAttr)),

            IPropertySymbol property => property.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, semanticAttr)),

            _ => false
        };
}
