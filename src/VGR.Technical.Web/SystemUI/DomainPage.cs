using System.Text;
using VGR.Semantics.Abstractions;

namespace VGR.Technical.Web.SystemUI;

/// <summary>
/// Renderar <c>/domain</c> — reflection-driven vy av domänens statiska struktur.
/// </summary>
internal static class DomainPage
{
    public static string Render(DomainModel model)
    {
        var body = new StringBuilder();

        // Gruppera per kind
        var groups = model.Types
            .GroupBy(t => t.Kind)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var kindLabel = KindLabel(group.Key);
            var kindCss = group.Key.ToString().ToLowerInvariant();

            body.AppendLine($"""<section class="kind-group {kindCss}">""");
            body.AppendLine($"<h2>{kindLabel}</h2>");

            foreach (var type in group.OrderBy(t => t.Name))
            {
                body.AppendLine($"""<details class="domain-type">""");
                body.AppendLine($"<summary><span class=\"type-name\">{Esc(type.Name)}</span> <span class=\"badge {kindCss}\">{kindLabel}</span></summary>");

                // Properties
                if (type.Properties.Count > 0)
                {
                    body.AppendLine("<div class=\"members\">");
                    body.AppendLine("<h3>Egenskaper</h3>");
                    body.AppendLine("<table><thead><tr><th>Namn</th><th>Typ</th><th></th></tr></thead><tbody>");
                    foreach (var p in type.Properties)
                    {
                        var markers = new List<string>();
                        if (p.IsReadOnly) markers.Add("<span class=\"tag ro\">readonly</span>");
                        if (p.HasSemanticQuery) markers.Add("<span class=\"tag sq\">[SemanticQuery]</span>");
                        var tags = string.Join(" ", markers);
                        body.AppendLine($"<tr><td class=\"member-name\">{Esc(p.Name)}</td><td class=\"member-type\">{Esc(p.TypeName)}</td><td>{tags}</td></tr>");
                    }
                    body.AppendLine("</tbody></table>");
                    body.AppendLine("</div>");
                }

                // Methods
                if (type.Methods.Count > 0)
                {
                    body.AppendLine("<div class=\"members\">");
                    body.AppendLine("<h3>Beteenden</h3>");
                    body.AppendLine("<table><thead><tr><th>Signatur</th><th></th></tr></thead><tbody>");
                    foreach (var m in type.Methods)
                    {
                        var ps = string.Join(", ", m.Parameters.Select(p => $"{Esc(p.TypeName)} {Esc(p.Name)}"));
                        var sig = $"{Esc(m.ReturnType)} {Esc(m.Name)}({ps})";

                        var markers = new List<string>();
                        if (m.IsStatic) markers.Add("<span class=\"tag st\">static</span>");
                        if (m.HasSemanticQuery) markers.Add("<span class=\"tag sq\">[SemanticQuery]</span>");
                        var tags = string.Join(" ", markers);

                        body.AppendLine($"<tr><td class=\"member-sig\">{sig}</td><td>{tags}</td></tr>");
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
                <title>Domän — VGR.Demo.Domain</title>
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

                    header a {
                        color: #0bd6ea;
                        text-decoration: none;
                        font-size: 0.85rem;
                    }

                    header h1 {
                        font-size: 1.5rem;
                        font-weight: 300;
                        color: #fff;
                        flex: 1;
                    }

                    .kind-group { margin-bottom: 2.5rem; }

                    .kind-group > h2 {
                        font-size: 1.1rem;
                        font-weight: 400;
                        color: #888;
                        text-transform: uppercase;
                        letter-spacing: 0.1em;
                        margin-bottom: 1rem;
                        padding-bottom: 0.25rem;
                        border-bottom: 1px solid #1a1a1a;
                    }

                    .domain-type {
                        margin-bottom: 0.75rem;
                        border: 1px solid #1a1a1a;
                        border-radius: 0.25rem;
                        background: #0f0f0f;
                    }

                    .domain-type > summary {
                        padding: 0.6rem 1rem;
                        cursor: pointer;
                        display: flex;
                        align-items: center;
                        gap: 0.75rem;
                        list-style: none;
                    }

                    .domain-type > summary::-webkit-details-marker { display: none; }

                    .domain-type > summary::before {
                        content: '\25b6';
                        font-size: 0.6rem;
                        color: #555;
                        transition: transform 0.15s;
                    }

                    .domain-type[open] > summary::before {
                        transform: rotate(90deg);
                    }

                    .type-name {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.95rem;
                        color: #fff;
                    }

                    .badge {
                        font-size: 0.65rem;
                        padding: 0.15rem 0.5rem;
                        border-radius: 0.15rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    }

                    .badge.aggregate { background: #1a3320; color: #4ade80; }
                    .badge.entity { background: #1a2733; color: #60a5fa; }
                    .badge.identity { background: #2a1a33; color: #c084fc; }
                    .badge.valueobject { background: #33291a; color: #fbbf24; }
                    .badge.domainevent { background: #331a1a; color: #f87171; }
                    .badge.exception { background: #33201a; color: #fb923c; }
                    .badge.static { background: #1a1a1a; color: #888; }

                    .members {
                        padding: 0.5rem 1rem 1rem;
                    }

                    .members h3 {
                        font-size: 0.75rem;
                        font-weight: 400;
                        color: #666;
                        text-transform: uppercase;
                        letter-spacing: 0.08em;
                        margin-bottom: 0.5rem;
                    }

                    table {
                        width: 100%;
                        border-collapse: collapse;
                        font-size: 0.85rem;
                    }

                    thead th {
                        text-align: left;
                        color: #555;
                        font-weight: 400;
                        font-size: 0.75rem;
                        padding: 0.25rem 0.5rem;
                        border-bottom: 1px solid #1a1a1a;
                    }

                    tbody td {
                        padding: 0.3rem 0.5rem;
                        border-bottom: 1px solid #111;
                    }

                    .member-name, .member-sig {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.8rem;
                        color: #ccc;
                    }

                    .member-type {
                        font-family: 'SF Mono', 'Cascadia Code', 'Fira Code', monospace;
                        font-size: 0.8rem;
                        color: #888;
                    }

                    .tag {
                        font-size: 0.6rem;
                        padding: 0.1rem 0.4rem;
                        border-radius: 0.15rem;
                        margin-left: 0.25rem;
                    }

                    .tag.ro { background: #1a1a2a; color: #818cf8; }
                    .tag.sq { background: #0a2a2a; color: #0bd6ea; }
                    .tag.st { background: #1a1a1a; color: #888; }

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
                    <h1>Domänstruktur</h1>
                    <a href="/">&larr; Tillbaka</a>
                </header>
                {{body}}
                <footer>
                    <a href="https://github.com/bemafred/sky-omega">
                        <img src="/edgar-badge.svg" alt="Sky Omega" title="Edgar">
                    </a>
                </footer>
            </body>
            </html>
            """;
    }

    private static string KindLabel(DomainTypeKind kind) => kind switch
    {
        DomainTypeKind.Aggregate => "Aggregat",
        DomainTypeKind.Entity => "Entiteter",
        DomainTypeKind.Identity => "Identiteter",
        DomainTypeKind.ValueObject => "Värdeobjekt",
        DomainTypeKind.DomainEvent => "Domänhändelser",
        DomainTypeKind.Exception => "Undantag",
        DomainTypeKind.Static => "Algoritmer & Fabriker",
        _ => kind.ToString()
    };

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
