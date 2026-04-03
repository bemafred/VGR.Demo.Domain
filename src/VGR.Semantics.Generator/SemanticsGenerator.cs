using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace VGR.Semantics.Generator;

/// <summary>
/// Compile-time validering av semantiska expansioner (ADR-011).
/// <para>
/// Körs som analyzer i <c>VGR.Infrastructure.EF</c> — det enda projektet som ser båda sidor:
/// <list type="bullet">
///   <item><c>[SemanticQuery]</c> i domäntyper (via metadata-referens till VGR.Domain)</item>
///   <item><c>[ExpansionFor]</c> i lokala expansionsfiler (via syntax trees)</item>
/// </list>
/// </para>
/// <para>
/// Diagnostiker:
/// <list type="bullet">
///   <item><b>SEMANTICS001</b> — <c>[SemanticQuery]</c>-medlem utan matchande <c>[ExpansionFor]</c> (Error)</item>
///   <item><b>SEMANTICS002</b> — <c>[ExpansionFor]</c> som pekar på icke-<c>[SemanticQuery]</c>-medlem (Warning)</item>
/// </list>
/// </para>
/// <para>Runtime wiring hanteras av <c>SemanticRegistry.DiscoverExpansions()</c> — generatorn är enbart diagnostik.</para>
/// </summary>
[Generator]
public sealed class SemanticGenerator : IIncrementalGenerator
{
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
        // [ExpansionFor]-metoder hittas via syntax trees (lokala filer i Infrastructure.EF)
        var expansionMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => (MethodDeclarationSyntax)ctx.Node)
            .Where(static m => m is not null);

        var combined = context.CompilationProvider
            .Combine(expansionMethods.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) =>
            Execute(spc, source.Left, source.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<MethodDeclarationSyntax> expansionMethodDeclarations)
    {
        var expansionAttr = compilation.GetTypeByMetadataName("VGR.Semantics.Abstractions.ExpansionForAttribute");
        var semanticAttr = compilation.GetTypeByMetadataName("VGR.Semantics.Abstractions.SemanticQueryAttribute");

        if (expansionAttr is null || semanticAttr is null) return;

        // 1) Bygg register över alla [SemanticQuery]-medlemmar via metadata-scanning.
        //    Dessa lever i refererade assemblies (VGR.Domain) — inte i lokala syntax trees.
        var semanticMembersDict = new Dictionary<string, ISymbol>();
        DiscoverSemanticMembers(compilation, semanticAttr, semanticMembersDict);

        // 2) Bygg register över alla [ExpansionFor]-mappningar via syntax trees (lokala filer)
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
                    var attrLocation = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? methodDecl.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        OrphanedExpansionRule, attrLocation,
                        factory.Name, targetType.Name, targetName));
                    continue;
                }

                var semanticCandidate = candidates.FirstOrDefault(c => IsSemanticMember(c, semanticAttr));

                if (semanticCandidate is null)
                {
                    var attrLocation = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? methodDecl.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        OrphanedExpansionRule, attrLocation,
                        factory.Name, targetType.Name, targetName));
                    continue;
                }

                mappedSemanticMembers.Add(GetMemberKey(semanticCandidate));
            }
        }

        // 3) Rapportera saknade expansioner för [SemanticQuery]-medlemmar
        foreach (var (key, symbol) in semanticMembersDict)
        {
            if (!mappedSemanticMembers.Contains(key))
            {
                // Metadata-symboler har inga source locations — rapportera utan plats
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingExpansionRule, Location.None,
                    symbol.ContainingType.Name, symbol.Name));
            }
        }
    }

    /// <summary>
    /// Skannar alla refererade assemblies (ej framework) efter typer med [SemanticQuery]-medlemmar.
    /// Hittar symboler via metadata — inte syntax trees — och fungerar därför cross-project.
    /// </summary>
    private static void DiscoverSemanticMembers(
        Compilation compilation,
        INamedTypeSymbol semanticAttr,
        Dictionary<string, ISymbol> result)
    {
        // Skanna kompilationens egna typer + alla refererade assemblies
        var allAssemblies = new List<IAssemblySymbol> { compilation.Assembly };
        allAssemblies.AddRange(
            compilation.References
                .Select(r => compilation.GetAssemblyOrModuleSymbol(r))
                .OfType<IAssemblySymbol>());

        foreach (var assembly in allAssemblies)
        {
            var name = assembly.Name;
            if (name.StartsWith("System") || name.StartsWith("Microsoft") ||
                name.StartsWith("netstandard") || name.StartsWith("mscorlib") ||
                name.StartsWith("xunit") || name.StartsWith("FluentAssertions"))
                continue;

            ScanNamespace(assembly.GlobalNamespace, semanticAttr, result);
        }
    }

    private static void ScanNamespace(
        INamespaceSymbol ns,
        INamedTypeSymbol semanticAttr,
        Dictionary<string, ISymbol> result)
    {
        foreach (var type in ns.GetTypeMembers())
            ScanType(type, semanticAttr, result);

        foreach (var child in ns.GetNamespaceMembers())
            ScanNamespace(child, semanticAttr, result);
    }

    private static void ScanType(
        INamedTypeSymbol type,
        INamedTypeSymbol semanticAttr,
        Dictionary<string, ISymbol> result)
    {
        foreach (var member in type.GetMembers())
        {
            if (IsSemanticMember(member, semanticAttr))
                result[GetMemberKey(member)] = member;
        }

        // Skanna nästlade typer
        foreach (var nested in type.GetTypeMembers())
            ScanType(nested, semanticAttr, result);
    }

    private static string GetMemberKey(ISymbol symbol)
    {
        var typeName = symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{typeName}::{symbol.Name}";
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
