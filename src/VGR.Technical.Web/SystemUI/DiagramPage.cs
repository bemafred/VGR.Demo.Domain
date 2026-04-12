using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using VGR.Semantics.Abstractions;
using VGR.Semantics.Linq;
using VGR.Technical.Web.Data;

namespace VGR.Technical.Web.SystemUI;

/// <summary>
/// Renderar <c>/diagrams</c> — självbeskrivande diagram genererade från domänens runtime-metadata.
/// Ren SVG utan externa beroenden. Diagrammen är systemet som beskriver sig självt.
/// </summary>
internal static class DiagramPage
{
    private static readonly DomainTypeKind[] ClassDiagramKinds =
    [
        DomainTypeKind.Aggregate,
        DomainTypeKind.Entity,
        DomainTypeKind.ValueObject
    ];

    private const double CharWidth = 7.0;
    private const double LineHeight = 18.0;
    private const double BoxPadding = 14.0;
    private const double HeaderLines = 2; // namn + stereotyp
    private const double SeparatorGap = 10.0;
    private const double GapX = 60.0;
    private const double GapY = 50.0;
    private const double SvgMargin = 30.0;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Render(DomainModel model, IServiceProvider services)
    {
        var (domainSvg, classPopups) = BuildDomainModelSvg(model, services);
        var expansionSvg = BuildExpansionSvg();
        var layerSvg = BuildLayerSvg();
        var layerRadialSvg = BuildLayerRadialSvg();

        return $$"""
            <!DOCTYPE html>
            <html lang="sv">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Diagram — VGR.Demo.Domain</title>
                <link rel="icon" href="/favicon.svg" type="image/svg+xml">
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }

                    body {
                        font-family: system-ui, -apple-system, sans-serif;
                        background: #0a0a0a;
                        color: #e0e0e0;
                        min-height: 100vh;
                        padding: 2rem;
                        max-width: none;
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

                    section { margin-bottom: 2.5rem; }

                    section > h2 {
                        font-size: 1.1rem;
                        font-weight: 400;
                        color: #888;
                        text-transform: uppercase;
                        letter-spacing: 0.1em;
                        margin-bottom: 1rem;
                        padding-bottom: 0.25rem;
                        border-bottom: 1px solid #1a1a1a;
                    }

                    .diagram-container {
                        background: #0f0f0f;
                        border: 1px solid #1a1a1a;
                        border-radius: 0.25rem;
                        padding: 1.5rem;
                        overflow-x: auto;
                        display: flex;
                        justify-content: center;
                    }

                    .legend {
                        display: flex;
                        gap: 1.5rem;
                        margin-top: 0.75rem;
                        font-size: 0.75rem;
                        color: #666;
                    }

                    .legend-item {
                        display: flex;
                        align-items: center;
                        gap: 0.35rem;
                    }

                    .legend-dot {
                        width: 10px;
                        height: 10px;
                        border-radius: 2px;
                        display: inline-block;
                    }

                    .view-toggle {
                        display: flex;
                        gap: 0.25rem;
                        margin-bottom: 0.75rem;
                    }

                    .view-toggle button {
                        background: #1a1a1a;
                        color: #666;
                        border: 1px solid #333;
                        padding: 0.3rem 0.8rem;
                        border-radius: 0.2rem;
                        font-size: 0.75rem;
                        cursor: pointer;
                        font-family: system-ui, sans-serif;
                        transition: all 0.15s;
                    }

                    .view-toggle button.active {
                        background: #0a2a2a;
                        color: #0bd6ea;
                        border-color: #0bd6ea;
                    }

                    .view-toggle button:hover:not(.active) {
                        border-color: #555;
                        color: #aaa;
                    }

                    .domain-diagram-section { position: relative; }

                    .class-popup {
                        display: none;
                        position: absolute;
                        top: 3.5rem;
                        left: 50%;
                        transform: translateX(-50%);
                        z-index: 10;
                        background: #0f0f0f;
                        border: 1px solid #333;
                        border-radius: 0.5rem;
                        padding: 1.5rem;
                        box-shadow: 0 8px 32px rgba(0,0,0,0.6);
                    }

                    .class-popup-close {
                        position: absolute;
                        top: 0.75rem;
                        right: 1rem;
                        background: none;
                        border: none;
                        color: #666;
                        font-size: 1.1rem;
                        cursor: pointer;
                        line-height: 1;
                    }

                    .class-popup-close:hover { color: #fff; }

                    .class-popup-backdrop {
                        display: none;
                        position: fixed;
                        inset: 0;
                        background: rgba(0,0,0,0.6);
                        z-index: 9;
                    }

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
                    <h1>Diagram</h1>
                    <a href="/">&larr; Tillbaka</a>
                </header>

                <section class="domain-diagram-section">
                    <h2>Domänmodell</h2>
                    <div class="diagram-container">
                        {{domainSvg}}
                    </div>
                    <div class="legend">
                        <span class="legend-item"><span class="legend-dot" style="background:#4ade80"></span> Aggregat</span>
                        <span class="legend-item"><span class="legend-dot" style="background:#60a5fa"></span> Entitet</span>
                        <span class="legend-item"><span class="legend-dot" style="background:#fbbf24"></span> Värdeobjekt</span>
                        <span class="legend-item" style="color:#0bd6ea">* = SemanticQuery</span>
                        <span class="legend-item" style="color:#666">Klicka en ruta för att förstora</span>
                    </div>
                    <div class="class-popup-backdrop" onclick="closeClassDetail()"></div>
                    {{classPopups}}
                </section>

                <script>
                    function openClassDetail(name) {
                        document.querySelector('.class-popup-backdrop').style.display = 'block';
                        document.getElementById('popup-' + name).style.display = 'block';
                    }
                    function closeClassDetail() {
                        document.querySelector('.class-popup-backdrop').style.display = 'none';
                        document.querySelectorAll('.class-popup').forEach(p => p.style.display = 'none');
                    }
                </script>

                <section>
                    <h2>Semantiska expansioner</h2>
                    <div class="diagram-container">
                        {{expansionSvg}}
                    </div>
                </section>

                <section>
                    <h2>Lagerstruktur</h2>
                    <nav class="view-toggle">
                        <button class="active" onclick="switchLayerView('linear')">Linjär</button>
                        <button onclick="switchLayerView('radial')">Koncentrisk</button>
                    </nav>
                    <div id="layer-linear" class="diagram-container">
                        {{layerSvg}}
                    </div>
                    <div id="layer-radial" class="diagram-container" style="display:none">
                        {{layerRadialSvg}}
                    </div>
                </section>

                <script>
                    function switchLayerView(view) {
                        document.getElementById('layer-linear').style.display = view === 'linear' ? 'flex' : 'none';
                        document.getElementById('layer-radial').style.display = view === 'radial' ? 'flex' : 'none';
                        document.querySelectorAll('.view-toggle button').forEach(b => b.classList.remove('active'));
                        event.target.classList.add('active');
                    }
                </script>

                <footer>
                    <a href="https://github.com/bemafred/sky-omega">
                        <img src="/edgar-badge.svg" alt="Sky Omega" title="Edgar">
                    </a>
                </footer>
            </body>
            </html>
            """;
    }

    // ── Diagram 1: Domänmodell ─────────────────────────────────────────

    private static (string svg, string popups) BuildDomainModelSvg(DomainModel model, IServiceProvider services)
    {
        var types = model.Types
            .Where(t => ClassDiagramKinds.Contains(t.Kind))
            .OrderBy(t => t.Kind).ThenBy(t => t.Name)
            .ToList();

        if (types.Count == 0) return ("<svg></svg>", "");

        // Beräkna dimensioner per typ
        var boxes = types.Select(t =>
        {
            var lines = GetVisibleLines(t);
            var maxLen = lines.Max(l => l.text.Length);
            maxLen = Math.Max(maxLen, t.Name.Length + 2);
            maxLen = Math.Max(maxLen, KindLabel(t.Kind).Length + 4);

            var w = maxLen * CharWidth + 2 * BoxPadding;
            var propCount = t.Properties.Count(p => p.Name != "RowVersion");
            var methodCount = t.Methods.Count;
            var h = (HeaderLines + propCount + methodCount) * LineHeight
                    + 2 * BoxPadding
                    + (propCount > 0 && methodCount > 0 ? SeparatorGap : 0)
                    + SeparatorGap; // separator efter header

            return new { Type = t, Width = w, Height = h, Lines = lines };
        }).ToList();

        // Extrahera relationer (behövs för layout)
        var typeNames = new HashSet<string>(types.Select(t => t.Name));
        var relations = new List<(string from, string to, string label, bool isCollection)>();
        var emitted = new HashSet<string>();

        foreach (var type in types)
        {
            foreach (var prop in type.Properties)
            {
                if (prop.Name == "RowVersion") continue;
                var raw = StripNullable(prop.TypeName);

                string? target = null;
                var isColl = false;
                if (IsCollectionType(raw))
                {
                    var el = ExtractCollectionElementType(raw);
                    if (el is not null && typeNames.Contains(el)) { target = el; isColl = true; }
                }
                else if (typeNames.Contains(raw) && raw != type.Name)
                {
                    target = raw;
                }

                if (target is not null && emitted.Add($"{type.Name}|{target}"))
                    relations.Add((type.Name, target, prop.Name, isColl));
            }
        }

        // EF-navigationer
        try
        {
            using var dbCtx = DbContextAccessor.GetReadContext(services);
            foreach (var type in types)
            {
                var clrType = DataEndpoints.ResolveClrType(type.FullName);
                if (clrType is null) continue;
                var entityType = dbCtx.Model.FindEntityType(clrType);
                if (entityType is null) continue;

                foreach (var nav in entityType.GetNavigations())
                {
                    var targetName = nav.TargetEntityType.ClrType.Name;
                    if (typeNames.Contains(targetName) && emitted.Add($"{type.Name}|{targetName}"))
                        relations.Add((type.Name, targetName, nav.Name, nav.IsCollection));
                }
            }
        }
        catch { /* DbContext ej tillgängligt */ }

        // Relationsdriven layout via BFS
        var adj = new Dictionary<string, List<string>>();
        var incoming = new HashSet<string>();
        foreach (var b in boxes) adj[b.Type.Name] = [];

        foreach (var (from, to, _, _) in relations)
        {
            if (adj.ContainsKey(from) && adj.ContainsKey(to))
            {
                adj[from].Add(to);
                incoming.Add(to);
            }
        }

        // BFS: tilldela kolumner
        var levels = new Dictionary<string, int>();
        var roots = boxes.Select(b => b.Type.Name).Where(n => !incoming.Contains(n)).ToList();
        if (roots.Count == 0) roots = [boxes[0].Type.Name];

        var bfsQueue = new Queue<string>(roots);
        foreach (var r in roots) levels[r] = 0;

        while (bfsQueue.Count > 0)
        {
            var name = bfsQueue.Dequeue();
            var nextCol = levels[name] + 1;
            foreach (var child in adj[name])
            {
                if (levels.ContainsKey(child)) continue;
                levels[child] = nextCol;
                bfsQueue.Enqueue(child);
            }
        }

        // Typer utan tilldelad nivå → samma kolumn som sin referent
        foreach (var b in boxes.Where(b => !levels.ContainsKey(b.Type.Name)))
        {
            var referent = relations
                .Where(r => r.to == b.Type.Name && levels.ContainsKey(r.from))
                .Select(r => levels[r.from])
                .FirstOrDefault();
            levels[b.Type.Name] = referent;
        }

        // Positionera: kolumner horisontellt, typer per kolumn vertikalt
        var columns = boxes.GroupBy(b => levels.GetValueOrDefault(b.Type.Name))
            .OrderBy(g => g.Key).ToList();

        var positions = new Dictionary<string, (double x, double y, double w, double h)>();
        var currentX = SvgMargin;

        foreach (var col in columns)
        {
            var colBoxes = col.ToList();
            var colWidth = colBoxes.Max(b => b.Width);
            var currentY = SvgMargin;

            foreach (var box in colBoxes)
            {
                positions[box.Type.Name] = (currentX, currentY, box.Width, box.Height);
                currentY += box.Height + GapY;
            }

            currentX += colWidth + GapX;
        }

        // SVG-dimensioner
        var svgW = positions.Values.Max(p => p.x + p.w) + SvgMargin;
        var svgH = positions.Values.Max(p => p.y + p.h) + SvgMargin;

        var svg = new StringBuilder();
        svg.AppendLine(F($"<svg viewBox='0 0 {svgW} {svgH}' xmlns='http://www.w3.org/2000/svg' width='100%'>"));
        svg.AppendLine(SvgDefs());

        // Rita relationspilar (först, så boxar ritas ovanpå)
        foreach (var (from, to, label, isCollection) in relations)
        {
            if (!positions.ContainsKey(from) || !positions.ContainsKey(to)) continue;
            var (fx, fy, fw, fh) = positions[from];
            var (tx, ty, tw, th) = positions[to];
            AppendRelation(svg, fx, fy, fw, fh, tx, ty, tw, th, label, isCollection);
        }

        // Rita klassrutor (klickbara)
        foreach (var box in boxes)
        {
            var (x, y, w, h) = positions[box.Type.Name];
            var (fill, stroke) = KindColors(box.Type.Kind);
            AppendClassBox(svg, box.Type, box.Lines, x, y, w, h, fill, stroke, clickable: true);
        }

        svg.AppendLine("</svg>");

        // Generera popup-divs med 2x förstorade klassrutor
        var popups = new StringBuilder();
        foreach (var box in boxes)
        {
            var (fill, stroke) = KindColors(box.Type.Kind);
            var pw = box.Width * 2;
            var ph = box.Height * 2;

            popups.AppendLine($"<div class='class-popup' id='popup-{Xml(box.Type.Name)}'>");
            popups.AppendLine("<button class='class-popup-close' onclick='closeClassDetail()'>&times;</button>");
            popups.AppendLine(F($"<svg viewBox='0 0 {box.Width + 2} {box.Height + 2}' width='{pw}' height='{ph}' xmlns='http://www.w3.org/2000/svg'>"));
            AppendClassBox(popups, box.Type, box.Lines, 1, 1, box.Width, box.Height, fill, stroke, clickable: false);
            popups.AppendLine("</svg>");
            popups.AppendLine("</div>");
        }

        return (svg.ToString(), popups.ToString());
    }

    private static List<(string text, bool isSemantic, bool isSeparator)> GetVisibleLines(DomainType type)
    {
        var lines = new List<(string text, bool isSemantic, bool isSeparator)>();
        foreach (var p in type.Properties)
        {
            if (p.Name == "RowVersion") continue;
            var sq = p.HasSemanticQuery ? " *" : "";
            lines.Add(($"+{p.TypeName} {p.Name}{sq}", p.HasSemanticQuery, false));
        }

        if (lines.Count > 0 && type.Methods.Count > 0)
            lines.Add(("", false, true));

        foreach (var m in type.Methods)
        {
            var ps = string.Join(", ", m.Parameters.Select(p => $"{p.TypeName} {p.Name}"));
            var stat = m.IsStatic ? " $" : "";
            var sq = m.HasSemanticQuery ? " *" : "";
            lines.Add(($"+{m.ReturnType} {m.Name}({ps}){stat}{sq}", m.HasSemanticQuery, false));
        }

        return lines;
    }

    private static void AppendClassBox(StringBuilder svg, DomainType type,
        List<(string text, bool isSemantic, bool isSeparator)> lines,
        double x, double y, double w, double h, string fill, string stroke, bool clickable = false)
    {
        var click = clickable ? $" style='cursor:pointer' onclick=\"openClassDetail('{Xml(type.Name)}')\"" : "";
        svg.AppendLine(F($"  <g data-type='{Xml(type.Name)}'{click}>"));

        // Bakgrund
        svg.AppendLine(F($"    <rect x='{x}' y='{y}' width='{w}' height='{h}' rx='4' fill='{fill}' stroke='{stroke}' stroke-width='1.5'/>"));

        // Typnamn
        var textX = x + w / 2;
        var textY = y + BoxPadding + 14;
        svg.AppendLine(F($"    <text x='{textX}' y='{textY}' text-anchor='middle' fill='#fff' font-family=\"'SF Mono','Cascadia Code',monospace\" font-size='13' font-weight='bold'>{Xml(type.Name)}</text>"));

        // Stereotyp
        textY += LineHeight;
        var stereo = $"\u00ab{KindLabel(type.Kind)}\u00bb";
        svg.AppendLine(F($"    <text x='{textX}' y='{textY}' text-anchor='middle' fill='#888' font-family='system-ui' font-size='10' font-style='italic'>{Xml(stereo)}</text>"));

        // Separator efter header
        textY += SeparatorGap / 2;
        svg.AppendLine(F($"    <line x1='{x + 6}' y1='{textY}' x2='{x + w - 6}' y2='{textY}' stroke='{stroke}' stroke-opacity='0.3' stroke-width='0.5'/>"));
        textY += SeparatorGap / 2;

        // Members
        var memberX = x + BoxPadding;
        foreach (var (text, isSemantic, isSeparator) in lines)
        {
            if (isSeparator)
            {
                textY += SeparatorGap / 2;
                svg.AppendLine(F($"    <line x1='{x + 6}' y1='{textY}' x2='{x + w - 6}' y2='{textY}' stroke='{stroke}' stroke-opacity='0.3' stroke-width='0.5'/>"));
                textY += SeparatorGap / 2;
                continue;
            }

            textY += LineHeight;
            var color = isSemantic ? "#0bd6ea" : "#ccc";
            svg.AppendLine(F($"    <text x='{memberX}' y='{textY}' fill='{color}' font-family=\"'SF Mono','Cascadia Code',monospace\" font-size='11'>{Xml(text)}</text>"));
        }

        // Expand-indikator
        if (clickable)
        {
            var ix = x + w - 16;
            var iy = y + 6;
            svg.AppendLine(F($"    <text x='{ix}' y='{iy + 10}' fill='{stroke}' font-family='system-ui' font-size='12' opacity='0.5'>&#x2197;</text>"));
        }

        svg.AppendLine("  </g>");
    }

    private static void AppendRelation(StringBuilder svg,
        double fx, double fy, double fw, double fh,
        double tx, double ty, double tw, double th, string label, bool isCollection)
    {
        // Kantpunktsval: välj närmaste sidor
        var edges = new (double x, double y)[]
        {
            (fx + fw, fy + fh / 2), // source right
            (fx, fy + fh / 2),       // source left
            (fx + fw / 2, fy + fh),  // source bottom
            (fx + fw / 2, fy),       // source top
        };

        var targets = new (double x, double y)[]
        {
            (tx, ty + th / 2),       // target left
            (tx + tw, ty + th / 2),  // target right
            (tx + tw / 2, ty),       // target top
            (tx + tw / 2, ty + th),  // target bottom
        };

        // Hitta paret med minst avstånd
        double bestDist = double.MaxValue;
        (double x, double y) bestSrc = edges[0], bestTgt = targets[0];

        foreach (var s in edges)
        foreach (var t in targets)
        {
            var d = Math.Sqrt((s.x - t.x) * (s.x - t.x) + (s.y - t.y) * (s.y - t.y));
            if (d < bestDist) { bestDist = d; bestSrc = s; bestTgt = t; }
        }

        var (x1, y1) = bestSrc;
        var (x2, y2) = bestTgt;

        // Ortogonal path: H → V → H
        var midX = (x1 + x2) / 2;
        svg.AppendLine(F($"  <path d='M {x1},{y1} H {midX} V {y2} H {x2}' fill='none' stroke='#555' stroke-width='1.5' marker-end='url(#arrow)'/>"));

        // Etikett vid mittpunkten
        var lx = midX;
        var ly = (y1 + y2) / 2 - 8;
        svg.AppendLine(F($"  <text x='{lx}' y='{ly}' text-anchor='middle' fill='#666' font-family='system-ui' font-size='10'>{Xml(label)}</text>"));

        // Kardinalitet
        svg.AppendLine(F($"  <text x='{x1 + (midX > x1 ? 8 : -8)}' y='{y1 - 6}' text-anchor='middle' fill='#555' font-family='system-ui' font-size='9'>1</text>"));
        var card = isCollection ? "*" : "1";
        svg.AppendLine(F($"  <text x='{x2 + (midX < x2 ? -8 : 8)}' y='{y2 - 6}' text-anchor='middle' fill='#555' font-family='system-ui' font-size='9'>{card}</text>"));
    }

    // ── Diagram 2: Semantiska expansioner ──────────────────────────────

    private static string BuildExpansionSvg()
    {
        var expansions = SemanticRegistry.GetExpansions();
        if (expansions.Count == 0) return "<svg></svg>";

        const double boxH = 36;
        const double boxPad = 12;
        const double arrowLen = 50;

        var sourceNodes = new Dictionary<MethodInfo, int>();
        var nodeLabels = new List<string>();
        var nodeIsSemantic = new List<bool>();
        var edges = new List<(int from, int to)>();

        // Skapa noder
        foreach (var (method, _) in expansions)
        {
            sourceNodes[method] = nodeLabels.Count;
            var typeName = method.DeclaringType?.Name ?? "?";
            var memberName = GetMemberName(method);
            var label = $"{typeName}.{memberName}";

            if (!method.IsSpecialName)
            {
                var lambda = expansions.First(e => e.Key == method).Value;
                var paramNames = string.Join(", ", lambda.Parameters.Select(p => p.Name));
                label += $"({paramNames})";
            }

            nodeLabels.Add(label);
            nodeIsSemantic.Add(true);
        }

        // Koppla kedjor
        foreach (var (method, lambda) in expansions)
        {
            var sourceId = sourceNodes[method];
            var collector = new MethodCollector();
            collector.Visit(lambda.Body);

            var chainTargets = collector.Found
                .Where(m => sourceNodes.ContainsKey(m))
                .Distinct()
                .ToList();

            if (chainTargets.Count > 0)
            {
                foreach (var target in chainTargets)
                    edges.Add((sourceId, sourceNodes[target]));
            }
            else
            {
                var termId = nodeLabels.Count;
                var expr = SimplifyExpression(lambda.Body.ToString());
                nodeLabels.Add(expr);
                nodeIsSemantic.Add(false);
                edges.Add((sourceId, termId));
            }
        }

        // Layout: horisontellt per kedja
        var nodeWidths = nodeLabels.Select(l => l.Length * CharWidth + 2 * boxPad).ToList();
        var placed = new Dictionary<int, (double x, double y)>();
        var visited = new HashSet<int>();

        // Hitta rotnoder (inga inkommande kanter)
        var hasIncoming = new HashSet<int>(edges.Select(e => e.to));
        var roots = Enumerable.Range(0, nodeLabels.Count).Where(i => !hasIncoming.Contains(i)).ToList();
        if (roots.Count == 0) roots = [0];

        var chainY = SvgMargin;
        foreach (var root in roots)
        {
            var chainX = SvgMargin;
            var queue = new Queue<int>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!visited.Add(id)) continue;

                placed[id] = (chainX, chainY);
                chainX += nodeWidths[id] + arrowLen;

                foreach (var (f, t) in edges.Where(e => e.from == id))
                    queue.Enqueue(t);
            }

            chainY += boxH + GapY;
        }

        // Placera eventuella oplacerade noder
        foreach (var id in Enumerable.Range(0, nodeLabels.Count).Where(i => !placed.ContainsKey(i)))
        {
            placed[id] = (SvgMargin, chainY);
            chainY += boxH + GapY;
        }

        var svgW = placed.Values.Max(p => p.x + nodeWidths[placed.First(kv => kv.Value == p).Key]) + SvgMargin;
        // Fix: calculate width properly
        var maxRight = placed.Max(kv => kv.Value.x + nodeWidths[kv.Key]);
        svgW = maxRight + SvgMargin;
        var svgH = placed.Values.Max(p => p.y) + boxH + SvgMargin;

        var svg = new StringBuilder();
        svg.AppendLine(F($"<svg viewBox='0 0 {svgW} {svgH}' xmlns='http://www.w3.org/2000/svg' style='max-width:{svgW}px'>"));
        svg.AppendLine(SvgDefs());

        // Rita noder
        for (var i = 0; i < nodeLabels.Count; i++)
        {
            if (!placed.ContainsKey(i)) continue;
            var (nx, ny) = placed[i];
            var nw = nodeWidths[i];
            var isSem = nodeIsSemantic[i];
            var fill = isSem ? "#0a2a2a" : "#1a1a1a";
            var stroke = isSem ? "#0bd6ea" : "#555";
            var rx = isSem ? "16" : "4";

            svg.AppendLine(F($"  <rect x='{nx}' y='{ny}' width='{nw}' height='{boxH}' rx='{rx}' fill='{fill}' stroke='{stroke}' stroke-width='1.5'/>"));
            svg.AppendLine(F($"  <text x='{nx + nw / 2}' y='{ny + boxH / 2 + 4}' text-anchor='middle' fill='{(isSem ? "#0bd6ea" : "#ccc")}' font-family=\"'SF Mono','Cascadia Code',monospace\" font-size='11'>{Xml(nodeLabels[i])}</text>"));
        }

        // Rita pilar
        foreach (var (from, to) in edges)
        {
            if (!placed.ContainsKey(from) || !placed.ContainsKey(to)) continue;
            var (fx, fy) = placed[from];
            var (tx, ty) = placed[to];
            var x1 = fx + nodeWidths[from];
            var y1 = fy + boxH / 2;
            var x2 = tx;
            var y2 = ty + boxH / 2;
            svg.AppendLine(F($"  <line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#555' stroke-width='1.5' marker-end='url(#arrow)'/>"));
        }

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    // ── Diagram 3: Lagerstruktur ───────────────────────────────────────

    private static string BuildLayerSvg()
    {
        var vgrAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("VGR.") == true)
            .Select(a => new
            {
                Name = a.GetName().Name!,
                References = a.GetReferencedAssemblies()
                    .Where(r => r.Name?.StartsWith("VGR.") == true)
                    .Select(r => r.Name!)
                    .ToList()
            })
            .OrderBy(a => a.Name)
            .ToList();

        if (vgrAssemblies.Count == 0) return "<svg></svg>";

        const double projBoxH = 28;
        const double projBoxPad = 12;
        const double projGap = 12;
        const double groupPad = 16;
        const double groupGap = 24;
        const double groupLabelH = 24;

        var groups = vgrAssemblies
            .GroupBy(a => ClassifyLayer(a.Name))
            .OrderBy(g => LayerOrder(g.Key))
            .ToList();

        // Jämn projektboxbredd baserad på det längsta namnet globalt
        var globalMaxNameLen = vgrAssemblies.Max(a => a.Name.Length);
        var projW = globalMaxNameLen * CharWidth + 2 * projBoxPad;

        // Radtilldelning: jämbördiga lager på samma rad
        var rowDef = new (int row, string[] layers)[]
        {
            (0, ["Leverans"]),
            (1, ["Applikation", "Infrastruktur", "Teknik"]),
            (2, ["Semantik"]),
            (3, ["Kärna"]),
        };

        // Beräkna gruppbredd per grupp (baserat på antal projekt)
        var groupWidths = new Dictionary<string, double>();
        foreach (var group in groups)
        {
            var count = group.Count();
            var contentW = count * (projW + projGap) - projGap;
            groupWidths[group.Key] = contentW + 2 * groupPad;
        }

        // Beräkna positioner — rader centrerade horisontellt
        var projPositions = new Dictionary<string, (double x, double y, double w)>();
        var groupBoxes = new List<(string label, double x, double y, double w, double h, string fill, string stroke)>();
        const double peerGap = 16;

        // Beräkna total bredd per rad för att hitta maxbredden
        var rowWidths = new List<double>();
        foreach (var (_, rowLayers) in rowDef)
        {
            var activeInRow = rowLayers.Where(l => groupWidths.ContainsKey(l)).ToList();
            if (activeInRow.Count == 0) { rowWidths.Add(0); continue; }
            var totalW = activeInRow.Sum(l => groupWidths[l]) + (activeInRow.Count - 1) * peerGap;
            rowWidths.Add(totalW);
        }

        var maxRowWidth = rowWidths.Max();
        var svgW = maxRowWidth + 2 * SvgMargin;

        var currentY = SvgMargin;
        for (var ri = 0; ri < rowDef.Length; ri++)
        {
            var (_, rowLayers) = rowDef[ri];
            var activeInRow = rowLayers.Where(l => groups.Any(g => g.Key == l)).ToList();
            if (activeInRow.Count == 0) continue;

            var groupH = groupLabelH + projBoxH + 2 * groupPad;
            var rowW = rowWidths[ri];
            var currentX = SvgMargin + (maxRowWidth - rowW) / 2; // Centrera raden

            foreach (var layerName in activeInRow)
            {
                var group = groups.First(g => g.Key == layerName);
                var items = group.OrderBy(a => a.Name).ToList();
                var gw = groupWidths[layerName];

                var (fill, stroke) = LayerColors(layerName);
                groupBoxes.Add((layerName, currentX, currentY, gw, groupH, fill, stroke));

                var projStartX = currentX + groupPad;
                var projY = currentY + groupLabelH + groupPad;
                foreach (var asm in items)
                {
                    projPositions[asm.Name] = (projStartX, projY, projW);
                    projStartX += projW + projGap;
                }

                currentX += gw + peerGap;
            }

            currentY += groupH + groupGap;
        }

        var svgH = currentY - groupGap + SvgMargin;

        var svg = new StringBuilder();
        svg.AppendLine(F($"<svg viewBox='0 0 {svgW} {svgH}' xmlns='http://www.w3.org/2000/svg' style='max-width:{svgW}px'>"));
        svg.AppendLine(SvgDefs());

        // Beroendepilar först (under boxarna)
        var loadedNames = new HashSet<string>(vgrAssemblies.Select(a => a.Name));
        foreach (var asm in vgrAssemblies)
        {
            foreach (var refName in asm.References)
            {
                if (!loadedNames.Contains(refName)) continue;
                if (ClassifyLayer(asm.Name) == ClassifyLayer(refName)) continue;

                var (fx, fy, fw) = projPositions[asm.Name];
                var (tx, ty, tw) = projPositions[refName];

                var x1 = fx + fw / 2;
                var y1 = fy + projBoxH;
                var x2 = tx + tw / 2;
                var y2 = ty;

                // Bézier-kurva: mjuk S-form
                var dy = (y2 - y1) * 0.4;
                svg.AppendLine(F($"  <path d='M {x1},{y1} C {x1},{y1 + dy} {x2},{y2 - dy} {x2},{y2}' fill='none' stroke='#333' stroke-width='1' stroke-opacity='0.6' marker-end='url(#arrow-small)'/>"));
            }
        }

        // Gruppboxar
        foreach (var (label, gx, gy, gw, gh, fill, stroke) in groupBoxes)
        {
            svg.AppendLine(F($"  <rect x='{gx}' y='{gy}' width='{gw}' height='{gh}' rx='6' fill='{fill}' stroke='{stroke}' stroke-width='1' stroke-opacity='0.5'/>"));
            svg.AppendLine(F($"  <text x='{gx + groupPad}' y='{gy + groupLabelH - 4}' fill='{stroke}' font-family='system-ui' font-size='11' font-weight='600'>{Xml(label)}</text>"));
        }

        // Projektboxar
        foreach (var asm in vgrAssemblies)
        {
            var (px, py, pw) = projPositions[asm.Name];
            var layer = ClassifyLayer(asm.Name);
            var (_, stroke) = LayerColors(layer);
            svg.AppendLine(F($"  <rect x='{px}' y='{py}' width='{pw}' height='{projBoxH}' rx='3' fill='#1a1a1a' stroke='{stroke}' stroke-width='1'/>"));
            svg.AppendLine(F($"  <text x='{px + pw / 2}' y='{py + projBoxH / 2 + 4}' text-anchor='middle' fill='#ccc' font-family=\"'SF Mono','Cascadia Code',monospace\" font-size='10'>{Xml(asm.Name)}</text>"));
        }

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    // ── Diagram 3b: Lagerstruktur — koncentrisk med sektioner ─────────

    private static string BuildLayerRadialSvg()
    {
        var vgrAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("VGR.") == true)
            .Select(a => a.GetName().Name!)
            .OrderBy(n => n)
            .ToList();

        if (vgrAssemblies.Count == 0) return "<svg></svg>";

        var byLayer = vgrAssemblies.GroupBy(ClassifyLayer)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n).ToList());

        // Ringdefinition: lager på samma ring är jämbördiga (inga inbördes beroenden)
        var rings = new (int ring, string[] layers)[]
        {
            (0, ["Kärna"]),
            (1, ["Semantik"]),
            (2, ["Applikation", "Infrastruktur", "Teknik"]),  // Jämbördiga — sektioner
            (3, ["Leverans"]),
        };

        const double coreRadius = 55;
        const double ringWidth = 52;
        const double ringGap = 5;
        const double sectionGapDeg = 2;

        var maxRing = rings.Max(r => r.ring);
        var outerRadius = coreRadius + (maxRing + 1) * (ringWidth + ringGap);
        var svgSize = outerRadius * 2 + 160;
        var centerX = svgSize / 2;
        var centerY = svgSize / 2;

        var svg = new StringBuilder();
        svg.AppendLine(F($"<svg viewBox='0 0 {svgSize} {svgSize}' xmlns='http://www.w3.org/2000/svg' style='max-width:{svgSize}px'>"));

        // Rita ringar utifrån och in (yttre först som bakgrund)
        for (var ri = rings.Length - 1; ri >= 0; ri--)
        {
            var (ring, layers) = rings[ri];
            var rInner = coreRadius + ring * (ringWidth + ringGap);
            var rOuter = rInner + ringWidth;

            var activeLayers = layers.Where(l => byLayer.ContainsKey(l)).ToList();
            if (activeLayers.Count == 0) continue;

            if (activeLayers.Count == 1)
            {
                // Full ring (annulus via path, inte fylld cirkel)
                var layer = activeLayers[0];
                var (fill, stroke) = LayerColors(layer);
                var ringPath = FullRingPath(centerX, centerY, rInner, rOuter);
                svg.AppendLine(F($"  <path d='{ringPath}' fill='{fill}' stroke='{stroke}' stroke-width='1' stroke-opacity='0.4'/>"));

                // Lageretikett + projektnamn — roterad text vid toppen av ringen
                var midDeg0 = -90.0;
                var labelR0 = rOuter - 12;
                AppendLayerLabel(svg, layer, byLayer[layer], centerX, centerY, labelR0, midDeg0, stroke);
            }
            else
            {
                // Sektioner — fördela 360 grader jämnt med gap
                var totalGap = activeLayers.Count * sectionGapDeg;
                var sectionSpan = (360.0 - totalGap) / activeLayers.Count;
                var currentAngle = -90.0;

                foreach (var layer in activeLayers)
                {
                    var startDeg = currentAngle + sectionGapDeg / 2;
                    var endDeg = startDeg + sectionSpan;
                    var (fill, stroke) = LayerColors(layer);

                    // Rita bågsektion
                    var path = ArcSectionPath(centerX, centerY, rInner, rOuter, startDeg, endDeg);
                    svg.AppendLine(F($"  <path d='{path}' fill='{fill}' stroke='{stroke}' stroke-width='1' stroke-opacity='0.4'/>"));

                    // Lageretikett + projektnamn — centrerat i sektionen
                    var midDeg = (startDeg + endDeg) / 2;
                    var labelR = rOuter - 12;
                    AppendLayerLabel(svg, layer, byLayer[layer], centerX, centerY, labelR, midDeg, stroke);

                    currentAngle = endDeg + sectionGapDeg / 2;
                }
            }

        }

        // Centrumcirkel — domänens suveränitet
        svg.AppendLine(F($"  <circle cx='{centerX}' cy='{centerY}' r='{coreRadius}' fill='#0f0f0f' stroke='#4ade80' stroke-width='1.5'/>"));
        svg.AppendLine(F($"  <text x='{centerX}' y='{centerY - 8}' text-anchor='middle' fill='#fff' font-family='system-ui' font-size='12' font-weight='bold'>E-Clean</text>"));
        svg.AppendLine(F($"  <text x='{centerX}' y='{centerY + 10}' text-anchor='middle' fill='#888' font-family='system-ui' font-size='9'>Domänen är</text>"));
        svg.AppendLine(F($"  <text x='{centerX}' y='{centerY + 22}' text-anchor='middle' fill='#888' font-family='system-ui' font-size='9'>suverän</text>"));

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    private static void AppendLayerLabel(StringBuilder svg, string layer, List<string> projects,
        double cx, double cy, double startR, double angleDeg, string stroke)
    {
        var rad = angleDeg * Math.PI / 180;
        var tangentDeg = angleDeg + 90;
        var normTangent = ((tangentDeg % 360) + 360) % 360;
        if (normTangent > 90 && normTangent < 270) tangentDeg += 180;

        const double lineStep = 13.0;

        // Lagernamn
        var r = startR;
        var tx = cx + r * Math.Cos(rad);
        var ty = cy + r * Math.Sin(rad);
        svg.AppendLine(F($"  <text x='{tx}' y='{ty}' text-anchor='middle' dominant-baseline='middle' fill='{stroke}' font-family='system-ui' font-size='10' font-weight='600' transform='rotate({tangentDeg},{tx},{ty})'>{Xml(layer)}</text>"));

        // Projektnamn — varje rad ett steg inåt (mindre radie)
        foreach (var proj in projects)
        {
            r -= lineStep;
            tx = cx + r * Math.Cos(rad);
            ty = cy + r * Math.Sin(rad);
            svg.AppendLine(F($"  <text x='{tx}' y='{ty}' text-anchor='middle' dominant-baseline='middle' fill='#ccc' font-family=\"'SF Mono','Cascadia Code',monospace\" font-size='8' transform='rotate({tangentDeg},{tx},{ty})'>{Xml(proj)}</text>"));
        }
    }

    private static void PlaceProjectNames(StringBuilder svg, List<string> items,
        double cx, double cy, double radius, double startDeg, double spanDeg)
    {
        if (items.Count == 0) return;
        var step = items.Count > 1 ? spanDeg / (items.Count + 1) : spanDeg / 2;

        for (var i = 0; i < items.Count; i++)
        {
            var angle = startDeg + step * (i + 1);
            var rad = angle * Math.PI / 180;
            var tx = cx + radius * Math.Cos(rad);
            var ty = cy + radius * Math.Sin(rad);
            var rot = angle > 90 || angle < -90 ? angle + 180 : angle;

            svg.AppendLine(F($"  <text x='{tx}' y='{ty}' text-anchor='middle' dominant-baseline='middle' fill='#ccc' font-family=\"'SF Mono','Cascadia Code',monospace\" font-size='8' transform='rotate({rot},{tx},{ty})'>{Xml(items[i])}</text>"));
        }
    }

    private static string FullRingPath(double cx, double cy, double rInner, double rOuter)
    {
        // Två halvcirklar — yttre CW, inre CCW — ger en sluten ring utan glipa
        var ol = cx - rOuter; var or2 = cx + rOuter;
        var il = cx - rInner; var ir = cx + rInner;
        return F($"M {or2},{cy} A {rOuter},{rOuter} 0 1 1 {ol},{cy} A {rOuter},{rOuter} 0 1 1 {or2},{cy} M {ir},{cy} A {rInner},{rInner} 0 1 0 {il},{cy} A {rInner},{rInner} 0 1 0 {ir},{cy} Z");
    }

    private static string LabelArcPath(double cx, double cy, double r, double startDeg, double endDeg, bool reverse)
    {
        // Vid reversering: byt start/slut så att CW-riktningen ger läsbar text i nedre halvan
        if (reverse) (startDeg, endDeg) = (endDeg, startDeg);

        var s = startDeg * Math.PI / 180;
        var e = endDeg * Math.PI / 180;
        var x1 = cx + r * Math.Cos(s);
        var y1 = cy + r * Math.Sin(s);
        var x2 = cx + r * Math.Cos(e);
        var y2 = cy + r * Math.Sin(e);

        // Alltid CW (sweep=1), alltid kort båge (large=0) — sektioner är < 180°
        return F($"M {x1},{y1} A {r},{r} 0 0 1 {x2},{y2}");
    }

    private static string ArcSectionPath(double cx, double cy, double rInner, double rOuter, double startDeg, double endDeg)
    {
        var s = startDeg * Math.PI / 180;
        var e = endDeg * Math.PI / 180;

        var x1o = cx + rOuter * Math.Cos(s);
        var y1o = cy + rOuter * Math.Sin(s);
        var x2o = cx + rOuter * Math.Cos(e);
        var y2o = cy + rOuter * Math.Sin(e);
        var x2i = cx + rInner * Math.Cos(e);
        var y2i = cy + rInner * Math.Sin(e);
        var x1i = cx + rInner * Math.Cos(s);
        var y1i = cy + rInner * Math.Sin(s);

        var large = (endDeg - startDeg) > 180 ? 1 : 0;

        return F($"M {x1o},{y1o} A {rOuter},{rOuter} 0 {large} 1 {x2o},{y2o} L {x2i},{y2i} A {rInner},{rInner} 0 {large} 0 {x1i},{y1i} Z");
    }

    // ── Gemensamma SVG-hjälpmetoder ────────────────────────────────────

    private static string SvgDefs() => """
          <defs>
            <marker id="arrow" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
              <polygon points="0 0, 10 3.5, 0 7" fill="#555"/>
            </marker>
            <marker id="arrow-small" markerWidth="7" markerHeight="5" refX="6" refY="2.5" orient="auto">
              <polygon points="0 0, 7 2.5, 0 5" fill="#333"/>
            </marker>
          </defs>
        """;

    private static (string fill, string stroke) KindColors(DomainTypeKind kind) => kind switch
    {
        DomainTypeKind.Aggregate => ("#1a3320", "#4ade80"),
        DomainTypeKind.Entity => ("#1a2733", "#60a5fa"),
        _ => ("#33291a", "#fbbf24")
    };

    private static (string fill, string stroke) LayerColors(string layer) => layer switch
    {
        "Kärna" => ("#0d1f14", "#4ade80"),
        "Semantik" => ("#1a0d24", "#c084fc"),
        "Applikation" => ("#0d1620", "#60a5fa"),
        "Infrastruktur" => ("#201510", "#fb923c"),
        "Teknik" => ("#10101f", "#818cf8"),
        "Leverans" => ("#0a1a1c", "#0bd6ea"),
        _ => ("#1a1a1a", "#888")
    };

    /// <summary>Formaterar double med punkt som decimaltecken.</summary>
    private static string F(FormattableString s) => s.ToString(Inv);

    /// <summary>XML-escapar text för attribut och innehåll.</summary>
    private static string Xml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ── Befintliga hjälpmetoder (oförändrade) ──────────────────────────

    private static string KindLabel(DomainTypeKind kind) => kind switch
    {
        DomainTypeKind.Aggregate => "Aggregat",
        DomainTypeKind.Entity => "Entitet",
        DomainTypeKind.ValueObject => "Värdeobjekt",
        _ => kind.ToString()
    };

    private static string StripNullable(string typeName)
        => typeName.EndsWith('?') ? typeName[..^1] : typeName;

    private static bool IsCollectionType(string typeName)
        => typeName.StartsWith("IReadOnlyList<")
        || typeName.StartsWith("IEnumerable<")
        || typeName.StartsWith("List<");

    private static string? ExtractCollectionElementType(string typeName)
    {
        var start = typeName.IndexOf('<');
        var end = typeName.LastIndexOf('>');
        return start >= 0 && end > start ? typeName[(start + 1)..end] : null;
    }

    private static string GetMemberName(MethodInfo method)
        => method.IsSpecialName && method.Name.StartsWith("get_")
            ? method.Name[4..]
            : method.Name;

    private static string SimplifyExpression(string expr)
    {
        var result = expr
            .Replace(" AndAlso ", " && ")
            .Replace(" OrElse ", " || ");

        result = Regex.Replace(result, @"Convert\((.+?),\s*[\w`]+\)", "$1");

        return result;
    }

    private static string ClassifyLayer(string assemblyName)
    {
        if (assemblyName is "VGR.Domain" || assemblyName.StartsWith("VGR.Domain."))
            return "Kärna";
        if (assemblyName.StartsWith("VGR.Semantics."))
            return "Semantik";
        if (assemblyName.StartsWith("VGR.Application"))
            return "Applikation";
        if (assemblyName.StartsWith("VGR.Infrastructure."))
            return "Infrastruktur";
        if (assemblyName.StartsWith("VGR.Technical"))
            return "Teknik";
        if (assemblyName.StartsWith("VGR.Web"))
            return "Leverans";
        return "Övrigt";
    }

    private static int LayerOrder(string layer) => layer switch
    {
        "Leverans" => 0,
        "Applikation" => 1,
        "Kärna" => 2,
        "Semantik" => 3,
        "Infrastruktur" => 4,
        "Teknik" => 5,
        _ => 9
    };

    private sealed class MethodCollector : ExpressionVisitor
    {
        public readonly List<MethodInfo> Found = [];

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo pi)
            {
                var getter = pi.GetGetMethod(true);
                if (getter is not null) Found.Add(getter);
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Found.Add(node.Method);
            return base.VisitMethodCall(node);
        }
    }
}
