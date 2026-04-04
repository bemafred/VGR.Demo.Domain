using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using VGR.Semantics.Linq;

namespace VGR.Technical.Web;

/// <summary>
/// Extension method som registrerar domänens webbyta.
/// Konsumerar metadata från <see cref="SemanticRegistry"/>.
/// </summary>
public static class DomainEndpoints
{
    /// <summary>
    /// Registrerar domänens webbyta: indexsida (<c>/</c>), <c>/domain</c>, favicon och assets.
    /// Kräver att <see cref="SemanticRegistry.UseDomain"/> har anropats först.
    /// </summary>
    public static WebApplication MapDomainEndpoints(this WebApplication app)
    {
        app.MapGet("/", () =>
        {
            var model = SemanticRegistry.GetModel();
            return Results.Content(IndexPage.Render(model), "text/html");
        }).ExcludeFromDescription();

        app.MapGet("/domain", () =>
        {
            var model = SemanticRegistry.GetModel();
            return Results.Content(DomainPage.Render(model), "text/html");
        }).ExcludeFromDescription();

        app.MapGet("/favicon.svg", () =>
            Results.Content(EmbeddedAssets.Read("edgar-favicon.svg"), "image/svg+xml"))
            .ExcludeFromDescription();

        app.MapGet("/edgar-badge.svg", () =>
            Results.Content(EmbeddedAssets.Read("edgar-badge.svg"), "image/svg+xml"))
            .ExcludeFromDescription();

        return app;
    }
}
