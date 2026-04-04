using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace VGR.Technical.Web;

/// <summary>
/// Extension method som registrerar domänens webbyta.
/// Anropas med <c>app.UseDomain(typeof(Region).Assembly)</c>.
/// </summary>
public static class DomainEndpoints
{
    private static Assembly[] _domainAssemblies = [];

    /// <summary>
    /// Registrerar domänens webbyta: indexsida (<c>/</c>), favicon och framtida <c>/domain</c>.
    /// </summary>
    public static WebApplication UseDomain(this WebApplication app, params Assembly[] domainAssemblies)
    {
        _domainAssemblies = domainAssemblies;

        app.MapGet("/", () => Results.Content(IndexPage.Render(domainAssemblies), "text/html"))
           .ExcludeFromDescription();

        app.MapGet("/favicon.svg", () =>
        {
            var svg = EmbeddedAssets.Read("edgar-favicon.svg");
            return Results.Content(svg, "image/svg+xml");
        }).ExcludeFromDescription();

        app.MapGet("/edgar-badge.svg", () =>
        {
            var svg = EmbeddedAssets.Read("edgar-badge.svg");
            return Results.Content(svg, "image/svg+xml");
        }).ExcludeFromDescription();

        return app;
    }
}
