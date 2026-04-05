using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace VGR.Technical.Web;

/// <summary>
/// Renderar <c>/api</c> — reflection-driven vy av registrerade HTTP-endpoints.
/// Extraherar metadata från ASP.NET Core:s <see cref="EndpointDataSource"/>.
/// </summary>
internal static class ApiPage
{
    public static string Render(IEnumerable<EndpointDataSource> dataSources)
    {
        var endpoints = dataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .Select(e =>
            {
                var action = e.Metadata.GetMetadata<ControllerActionDescriptor>()!;
                var httpMethods = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
                var produces = e.Metadata.OfType<ProducesResponseTypeAttribute>().ToList();

                return new
                {
                    Route = e.RoutePattern.RawText ?? "?",
                    Methods = httpMethods,
                    Controller = action.ControllerName,
                    Action = action.ActionName,
                    Parameters = action.Parameters
                        .Where(p => p.ParameterType != typeof(CancellationToken))
                        .Select(p => new
                    {
                        p.Name,
                        Type = FormatTypeName(p.ParameterType),
                        Source = p.BindingInfo?.BindingSource?.DisplayName ?? "unknown",
                        DtoFields = ExtractDtoFields(p.ParameterType, p.BindingInfo?.BindingSource?.DisplayName)
                    }).ToList(),
                    Produces = produces
                        .GroupBy(p => p.StatusCode)
                        .Select(g => new
                        {
                            g.Key,
                            Type = g.Select(p => p.Type).FirstOrDefault(t => t is not null && t != typeof(void)) is { } t
                                ? FormatTypeName(t) : null
                        })
                        .Select(g => new { StatusCode = g.Key, g.Type })
                        .ToList(),
                    ReturnType = FormatTypeName(action.MethodInfo.ReturnType)
                };
            })
            .OrderBy(e => e.Route)
            .ThenBy(e => e.Methods.FirstOrDefault())
            .ToList();

        var body = new StringBuilder();

        if (endpoints.Count == 0)
        {
            body.AppendLine("<p class=\"empty\">Inga API-endpoints registrerade.</p>");
        }

        // Gruppera per controller
        var groups = endpoints.GroupBy(e => e.Controller);

        foreach (var group in groups)
        {
            body.AppendLine($"""<section class="controller-group">""");
            body.AppendLine($"<h2>{Esc(group.Key)}</h2>");

            foreach (var ep in group)
            {
                var methods = string.Join(" ", ep.Methods.Select(m =>
                    $"<span class=\"method {m.ToLowerInvariant()}\">{Esc(m)}</span>"));

                body.AppendLine($"""<details class="endpoint" open>""");
                body.AppendLine($"<summary>{methods} <span class=\"route\">{Esc(ep.Route)}</span></summary>");

                // Parameters
                var bodyParams = ep.Parameters.Where(p => p.Source is not "Path" and not "unknown").ToList();
                var pathParams = ep.Parameters.Where(p => p.Source == "Path").ToList();

                if (pathParams.Count > 0 || bodyParams.Count > 0)
                {
                    body.AppendLine("<div class=\"details\">");

                    if (pathParams.Count > 0)
                    {
                        body.AppendLine("<h3>Path-parametrar</h3>");
                        body.AppendLine("<table><tbody>");
                        foreach (var p in pathParams)
                            body.AppendLine($"<tr><td class=\"param-name\">{Esc(p.Name)}</td><td class=\"param-type\">{Esc(p.Type)}</td></tr>");
                        body.AppendLine("</tbody></table>");
                    }

                    if (bodyParams.Count > 0)
                    {
                        foreach (var p in bodyParams)
                        {
                            body.AppendLine($"<h3>Body — <span class=\"param-type\">{Esc(p.Type)}</span></h3>");
                            if (p.DtoFields.Count > 0)
                            {
                                body.AppendLine("<table><thead><tr><th>Fält</th><th>Typ</th><th></th></tr></thead><tbody>");
                                foreach (var f in p.DtoFields)
                                {
                                    var tags = string.Join(" ", f.Tags.Select(t =>
                                        $"<span class=\"tag {t.Css}\">{Esc(t.Label)}</span>"));
                                    body.AppendLine($"<tr><td class=\"param-name\">{Esc(f.Name)}</td><td class=\"param-type\">{Esc(f.Type)}</td><td>{tags}</td></tr>");
                                }
                                body.AppendLine("</tbody></table>");
                            }
                        }
                    }

                    body.AppendLine("</div>");
                }

                // Response codes
                if (ep.Produces.Count > 0)
                {
                    body.AppendLine("<div class=\"details\">");
                    body.AppendLine("<h3>Response</h3>");
                    body.AppendLine("<table><tbody>");
                    foreach (var p in ep.Produces.OrderBy(p => p.StatusCode))
                    {
                        var statusClass = p.StatusCode < 300 ? "ok" : p.StatusCode < 400 ? "redirect" : "error";
                        var typeStr = p.Type is not null ? Esc(p.Type) : "";
                        body.AppendLine($"<tr><td class=\"status {statusClass}\">{p.StatusCode}</td><td class=\"param-type\">{typeStr}</td></tr>");
                    }
                    body.AppendLine("</tbody></table>");
                    body.AppendLine("</div>");
                }

                body.AppendLine("</details>");
            }

            body.AppendLine("</section>");
        }

        return $$"""
            <!DOCTYPE html>
            <html lang="sv">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>API — VGR.Demo.Domain</title>
                <link rel="icon" href="/favicon.svg" type="image/svg+xml">
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }

                    body {
                        font-family: system-ui, -apple-system, sans-serif;
                        background: #0a0a0a;
                        color: #e0e0e0;
                        min-height: 100vh;
                        padding: 2rem;
                        max-width: 72rem;
                        margin: 0 auto;
                    }

                    header {
                        display: flex;
                        align-items: center;
                        gap: 1rem;
                        margin-bottom: 2rem;
                        padding-bottom: 1rem;
                        border-bottom: 1px solid #1a1a1a;
                    }

                    header a { color: #0bd6ea; text-decoration: none; font-size: 0.85rem; }
                    header h1 { font-size: 1.5rem; font-weight: 300; color: #fff; flex: 1; }

                    .controller-group { margin-bottom: 2.5rem; }

                    .controller-group > h2 {
                        font-size: 1.1rem;
                        font-weight: 400;
                        color: #888;
                        text-transform: uppercase;
                        letter-spacing: 0.1em;
                        margin-bottom: 1rem;
                        padding-bottom: 0.25rem;
                        border-bottom: 1px solid #1a1a1a;
                    }

                    .endpoint {
                        margin-bottom: 0.75rem;
                        border: 1px solid #1a1a1a;
                        border-radius: 0.25rem;
                        background: #0f0f0f;
                    }

                    .endpoint > summary {
                        padding: 0.6rem 1rem;
                        cursor: pointer;
                        display: flex;
                        align-items: center;
                        gap: 0.75rem;
                        list-style: none;
                    }

                    .endpoint > summary::-webkit-details-marker { display: none; }

                    .endpoint > summary::before {
                        content: '\25b6';
                        font-size: 0.6rem;
                        color: #555;
                        transition: transform 0.15s;
                    }

                    .endpoint[open] > summary::before { transform: rotate(90deg); }

                    .method {
                        font-size: 0.7rem;
                        font-weight: 600;
                        padding: 0.15rem 0.5rem;
                        border-radius: 0.15rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    }

                    .method.get { background: #1a2733; color: #60a5fa; }
                    .method.post { background: #1a3320; color: #4ade80; }
                    .method.put { background: #33291a; color: #fbbf24; }
                    .method.delete { background: #331a1a; color: #f87171; }
                    .method.patch { background: #2a1a33; color: #c084fc; }

                    .route {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.9rem;
                        color: #fff;
                    }

                    .details { padding: 0.5rem 1rem 1rem; }

                    .details h3 {
                        font-size: 0.75rem;
                        font-weight: 400;
                        color: #666;
                        text-transform: uppercase;
                        letter-spacing: 0.08em;
                        margin-bottom: 0.5rem;
                        margin-top: 0.5rem;
                    }

                    table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }

                    tbody td {
                        padding: 0.3rem 0.5rem;
                        border-bottom: 1px solid #111;
                    }

                    .param-name {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.8rem;
                        color: #ccc;
                    }

                    .param-type {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.8rem;
                        color: #888;
                    }

                    .status {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.8rem;
                        font-weight: 600;
                    }

                    .status.ok { color: #4ade80; }
                    .status.redirect { color: #fbbf24; }
                    .status.error { color: #f87171; }

                    .tag { font-size: 0.6rem; padding: 0.1rem 0.4rem; border-radius: 0.15rem; margin-left: 0.25rem; }
                    .tag.req { background: #331a1a; color: #f87171; }
                    .tag.len { background: #1a1a2a; color: #818cf8; }
                    .tag.opt { background: #1a2a1a; color: #86efac; }

                    .empty { color: #555; font-style: italic; padding: 2rem 0; }

                    footer {
                        margin-top: 3rem;
                        padding-top: 1rem;
                        border-top: 1px solid #1a1a1a;
                        text-align: center;
                        opacity: 0.4;
                        transition: opacity 0.3s;
                    }

                    footer:hover { opacity: 0.8; }
                    footer img { width: 36px; height: 36px; }
                </style>
            </head>
            <body>
                <header>
                    <h1>API-endpoints</h1>
                    <a href="/">&larr; Tillbaka</a>
                </header>
                {{body}}
                <footer>
                    <a href="https://github.com/bemafred/sky-omega">
                        <img src="/edgar-badge.svg" alt="Sky Omega">
                    </a>
                </footer>
            </body>
            </html>
            """;
    }

    private record DtoFieldTag(string Label, string Css);
    private record DtoField(string Name, string Type, List<DtoFieldTag> Tags);

    private static List<DtoField> ExtractDtoFields(Type type, string? source)
    {
        // Bara expandera body-parametrar (DTO:er), inte path/query-parametrar
        if (source is not "Body") return [];
        if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid)) return [];

        return type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(p =>
            {
                var tags = new List<DtoFieldTag>();

                // Required
                if (p.GetCustomAttributes(true).Any(a => a.GetType().Name == "RequiredAttribute"))
                    tags.Add(new DtoFieldTag("required", "req"));

                // StringLength
                var strLen = p.GetCustomAttributes(true)
                    .FirstOrDefault(a => a.GetType().Name == "StringLengthAttribute");
                if (strLen is not null)
                {
                    var max = (int)strLen.GetType().GetProperty("MaximumLength")!.GetValue(strLen)!;
                    var min = (int)strLen.GetType().GetProperty("MinimumLength")!.GetValue(strLen)!;
                    tags.Add(new DtoFieldTag($"{min}–{max}", "len"));
                }

                // Nullable
                var underlying = Nullable.GetUnderlyingType(p.PropertyType);
                if (underlying is not null)
                    tags.Add(new DtoFieldTag("nullable", "opt"));

                return new DtoField(p.Name, FormatTypeName(p.PropertyType), tags);
            })
            .ToList();
    }

    private static string FormatTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(Guid)) return "Guid";

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
            return FormatTypeName(underlying) + "?";

        if (type.IsGenericType)
        {
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick > 0) name = name[..tick];
            return $"{name}<{args}>";
        }

        return type.Name;
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
