using System.Collections;
using System.Reflection;
using System.Text;
using VGR.Semantics.Abstractions;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Renderar <c>GET /data/{type}</c> — listar alla instanser av en aggregat/entitetstyp.
/// </summary>
internal static class DataInstancesPage
{
    public static string Render(string typeName, DomainType domainType, IList instances, Type clrType)
    {
        var body = new StringBuilder();

        // Identifiera kolumner: skalarproperties (ej navigationslistor)
        var props = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !typeof(IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string) || p.PropertyType == typeof(byte[]))
            .Where(p => p.Name != "RowVersion")
            .ToList();

        body.AppendLine("<table>");
        body.AppendLine("<thead><tr>");
        foreach (var p in props)
            body.AppendLine($"<th>{DataLayout.Esc(p.Name)}</th>");
        body.AppendLine("</tr></thead>");
        body.AppendLine("<tbody>");

        foreach (var entity in instances)
        {
            body.AppendLine("<tr>");
            foreach (var p in props)
            {
                var val = p.GetValue(entity);
                var display = FormatValue(val);

                if (p.Name == "Id")
                {
                    var idGuid = ExtractGuid(val);
                    body.AppendLine($"""<td class="mono"><a class="entity-link" href="/data/{DataLayout.Esc(typeName)}/{idGuid}">{DataLayout.Esc(display)}</a></td>""");
                }
                else
                    body.AppendLine($"""<td class="mono">{DataLayout.Esc(display)}</td>""");
            }
            body.AppendLine("</tr>");
        }

        body.AppendLine("</tbody></table>");

        if (instances.Count == 0)
            body.AppendLine("""<p class="empty">Inga instanser hittades.</p>""");

        // Statiska fabriksmetoder — formulär
        var staticMethods = domainType.Methods.Where(m => m.IsStatic).ToList();
        if (staticMethods.Count > 0)
        {
            body.AppendLine("<section>");
            body.AppendLine("<h2>Skapa</h2>");

            foreach (var m in staticMethods)
            {
                var formId = $"form-{m.Name}";
                var ps = string.Join(", ", m.Parameters.Select(p => $"{DataLayout.Esc(p.TypeName)} {DataLayout.Esc(p.Name)}"));
                body.AppendLine($"""<div class="method-form" id="{formId}-container">""");
                body.AppendLine($"<h3>{DataLayout.Esc(m.ReturnType)} {DataLayout.Esc(m.Name)}({ps})</h3>");
                body.AppendLine($"""<form id="{formId}" data-url="/data/{DataLayout.Esc(typeName)}/{DataLayout.Esc(m.Name)}">""");

                foreach (var p in m.Parameters)
                {
                    body.AppendLine($"""<label for="{formId}-{DataLayout.Esc(p.Name)}">{DataLayout.Esc(p.Name)} <span style="color:#555">({DataLayout.Esc(p.TypeName)})</span></label>""");
                    body.AppendLine($"""<input id="{formId}-{DataLayout.Esc(p.Name)}" name="{DataLayout.Esc(p.Name)}" type="{InputType(p.TypeName)}" placeholder="{DataLayout.Esc(p.TypeName)}" required>""");
                }

                body.AppendLine("""<button type="submit">Skapa</button>""");
                body.AppendLine($"""<div class="result-banner" id="{formId}-result"></div>""");
                body.AppendLine("</form>");
                body.AppendLine("</div>");
            }

            body.AppendLine("</section>");
        }

        body.AppendLine(MethodFormScript());

        return DataLayout.Wrap(
            typeName,
            $"""<a href="/">Hem</a> / <a href="/data">Data</a> / <strong>{DataLayout.Esc(typeName)}</strong>""",
            body.ToString());
    }

    private static string InputType(string typeName) => typeName switch
    {
        "DateTimeOffset" => "datetime-local",
        "DateOnly" => "date",
        "int" or "long" or "decimal" or "double" => "number",
        "bool" => "checkbox",
        "Guid" => "text",
        _ => "text"
    };

    private static string MethodFormScript() => """
        <script>
        document.querySelectorAll('.method-form form').forEach(form => {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const url = form.dataset.url;
                const banner = document.getElementById(form.id + '-result');
                banner.className = 'result-banner';
                banner.style.display = 'none';
                banner.textContent = '';

                const data = {};
                new FormData(form).forEach((v, k) => { data[k] = v; });

                try {
                    const res = await fetch(url, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(data)
                    });

                    const text = await res.text();
                    let body;
                    try { body = JSON.parse(text); } catch { body = text; }

                    if (res.ok) {
                        banner.className = 'result-banner success';
                        banner.textContent = typeof body === 'object' ? JSON.stringify(body, null, 2) : body;
                        banner.style.display = 'block';
                        if (body.url) setTimeout(() => location.href = body.url, 1200);
                        else setTimeout(() => location.reload(), 1200);
                    } else {
                        banner.className = 'result-banner error';
                        const detail = body.detail || body.title || text;
                        const code = body.extensions?.code || body.code || '';
                        banner.textContent = `${res.status}: ${detail}${code ? ' [' + code + ']' : ''}`;
                        banner.style.display = 'block';
                    }
                } catch (err) {
                    banner.className = 'result-banner error';
                    banner.textContent = err.message;
                    banner.style.display = 'block';
                }
            });
        });
        </script>
        """;

    private static string FormatValue(object? val) => val switch
    {
        null => "–",
        byte[] => "[bytes]",
        DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss"),
        _ => val.ToString() ?? "–"
    };

    private static string ExtractGuid(object? val)
    {
        if (val is null) return "";
        // Identity record structs har en Value-property av typen Guid
        var valueProp = val.GetType().GetProperty("Value");
        if (valueProp is not null && valueProp.PropertyType == typeof(Guid))
            return ((Guid)valueProp.GetValue(val)!).ToString();
        return val.ToString() ?? "";
    }
}
