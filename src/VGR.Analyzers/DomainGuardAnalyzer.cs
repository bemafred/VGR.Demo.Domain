
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VGR.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainGuardAnalyzer : DiagnosticAnalyzer
{
    public const string NoPublicSetterId = "VGR001";
    public const string NoMutableCollectionsId = "VGR002";

    private static readonly DiagnosticDescriptor NoPublicSetterRule = new(
        id: NoPublicSetterId,
        title: "Domän-egenskaper får inte ha public set",
        messageFormat: "Egenskapen '{0}' i domänen har en public setter. Gör den privat eller ta bort set och exponera beteende istället.",
        category: "Domain",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Domänens offentliga egenskaper ska vara read-only; mutation sker via beteendemönster/fabriker."
    );

    private static readonly DiagnosticDescriptor NoMutableCollectionsRule = new(
        id: NoMutableCollectionsId,
        title: "Exponera inte muterbara samlingar i domänen",
        messageFormat: "Egenskapen '{0}' exponerar '{1}'. Använd privat backing field + IReadOnlyList<T> istället.",
        category: "Domain",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Domänen bör inte exponera ICollection<T>/IList<T>/List<T> publikt."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(NoPublicSetterRule, NoMutableCollectionsRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext ctx)
    {
        // Gäller bara projekt/namnrymder som ser ut som domän
        var ns = ctx.ContainingSymbol?.ContainingNamespace?.ToDisplayString() ?? "";
        if (!ns.Contains("VGR.Domain")) return;

        var prop = (PropertyDeclarationSyntax)ctx.Node;

        // Skip if not public
        if (!prop.Modifiers.Any(SyntaxKind.PublicKeyword)) return;

        // 1) Public setter?
        var acc = prop.AccessorList;
        if (acc is not null)
        {
            foreach (var a in acc.Accessors)
            {
                if (a.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    var hasPublicSet = !a.Modifiers.Any() || a.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
                    if (hasPublicSet)
                    {
                        var diag = Diagnostic.Create(NoPublicSetterRule, a.GetLocation(), prop.Identifier.Text);
                        ctx.ReportDiagnostic(diag);
                        break;
                    }
                }
            }
        }

        // 2) Mutable collection exposed?
        var type = ctx.SemanticModel.GetTypeInfo(prop.Type).Type;
        if (type is null) return;

        bool IsMutableCollection(ITypeSymbol t)
        {
            if (t is INamedTypeSymbol nt)
            {
                var name = nt.ConstructedFrom.ToDisplayString();
                if (name is "System.Collections.Generic.ICollection<T>" or
                           "System.Collections.Generic.IList<T>" or
                           "System.Collections.Generic.List<T>" )
                    return true;
            }
            return false;
        }

        if (IsMutableCollection(type))
        {
            var diag = Diagnostic.Create(NoMutableCollectionsRule, prop.Type.GetLocation(), prop.Identifier.Text, type.ToDisplayString());
            ctx.ReportDiagnostic(diag);
        }
    }
}
