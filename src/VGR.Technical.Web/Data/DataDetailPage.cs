using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VGR.Semantics.Abstractions;
using VGR.Semantics.Linq;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Renderar <c>GET /data/{type}/{id}</c> — visar en enskild instans med alla properties och navigationslänkar.
/// </summary>
internal static class DataDetailPage
{
    public static string Render(string typeName, string idString, object entity, Type clrType, DbContext ctx)
    {
        var body = new StringBuilder();

        // Skalärproperties
        body.AppendLine("<section>");
        body.AppendLine("<h2>Egenskaper</h2>");
        body.AppendLine("<table>");
        body.AppendLine("<thead><tr><th>Egenskap</th><th>Typ</th><th>Värde</th></tr></thead>");
        body.AppendLine("<tbody>");

        var props = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {
            if (p.Name == "RowVersion") continue;

            var isNavCollection = typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                                  && p.PropertyType != typeof(string)
                                  && p.PropertyType != typeof(byte[]);

            if (isNavCollection) continue; // visas separat

            var val = p.GetValue(entity);
            var display = FormatValue(val);
            var typLabel = FormatTypeName(p.PropertyType);

            body.AppendLine("<tr>");
            body.AppendLine($"""  <td class="mono">{DataLayout.Esc(p.Name)}</td>""");
            body.AppendLine($"""  <td class="mono" style="color:#888">{DataLayout.Esc(typLabel)}</td>""");
            body.AppendLine($"""  <td class="mono">{DataLayout.Esc(display)}</td>""");
            body.AppendLine("</tr>");
        }

        body.AppendLine("</tbody></table>");
        body.AppendLine("</section>");

        // Navigationer
        var entityTypeModel = ctx.Model.FindEntityType(clrType);
        if (entityTypeModel is not null)
        {
            var navigations = entityTypeModel.GetNavigations().ToList();
            if (navigations.Count > 0)
            {
                body.AppendLine("<section>");
                body.AppendLine("<h2>Relationer</h2>");
                body.AppendLine("<table>");
                body.AppendLine("<thead><tr><th>Relation</th><th>Typ</th><th>Antal / Värde</th><th></th></tr></thead>");
                body.AppendLine("<tbody>");

                foreach (var nav in navigations)
                {
                    var navProp = clrType.GetProperty(nav.Name);
                    if (navProp is null) continue;

                    var navVal = navProp.GetValue(entity);

                    if (nav.IsCollection)
                    {
                        var count = navVal is ICollection col ? col.Count : 0;
                        body.AppendLine("<tr>");
                        body.AppendLine($"""  <td class="mono">{DataLayout.Esc(nav.Name)}</td>""");
                        body.AppendLine($"""  <td class="mono" style="color:#888">{DataLayout.Esc(nav.TargetEntityType.ClrType.Name)}[]</td>""");
                        body.AppendLine($"""  <td>{count} st</td>""");
                        body.AppendLine($"""  <td><a class="entity-link" href="/data/{DataLayout.Esc(typeName)}/{DataLayout.Esc(idString)}/{DataLayout.Esc(nav.Name)}">Visa &rarr;</a></td>""");
                        body.AppendLine("</tr>");
                    }
                    else
                    {
                        var display = navVal is null ? "–" : FormatValue(GetId(navVal));
                        body.AppendLine("<tr>");
                        body.AppendLine($"""  <td class="mono">{DataLayout.Esc(nav.Name)}</td>""");
                        body.AppendLine($"""  <td class="mono" style="color:#888">{DataLayout.Esc(nav.TargetEntityType.ClrType.Name)}</td>""");
                        body.AppendLine($"""  <td class="mono">{DataLayout.Esc(display)}</td>""");
                        if (navVal is not null)
                        {
                            var targetId = GetIdGuid(navVal);
                            body.AppendLine($"""  <td><a class="entity-link" href="/data/{DataLayout.Esc(nav.TargetEntityType.ClrType.Name)}/{targetId}">Visa &rarr;</a></td>""");
                        }
                        else
                            body.AppendLine("  <td></td>");
                        body.AppendLine("</tr>");
                    }
                }

                body.AppendLine("</tbody></table>");
                body.AppendLine("</section>");
            }
        }

        // Instansmetoder — formulär
        var model = SemanticRegistry.GetModel();
        var domainType = model.Types.FirstOrDefault(t => t.Name == typeName);
        if (domainType is not null)
        {
            var instanceMethods = domainType.Methods.Where(m => !m.IsStatic).ToList();
            if (instanceMethods.Count > 0)
            {
                body.AppendLine("<section>");
                body.AppendLine("<h2>Beteenden</h2>");

                foreach (var m in instanceMethods)
                {
                    var formId = $"form-{m.Name}";
                    var ps = string.Join(", ", m.Parameters.Select(p => $"{DataLayout.Esc(p.TypeName)} {DataLayout.Esc(p.Name)}"));
                    body.AppendLine($"""<div class="method-form" id="{formId}-container">""");
                    body.AppendLine($"<h3>{DataLayout.Esc(m.ReturnType)} {DataLayout.Esc(m.Name)}({ps})</h3>");
                    body.AppendLine($"""<form id="{formId}" data-url="/data/{DataLayout.Esc(typeName)}/{DataLayout.Esc(idString)}/{DataLayout.Esc(m.Name)}">""");

                    foreach (var p in m.Parameters)
                    {
                        body.AppendLine($"""<label for="{formId}-{DataLayout.Esc(p.Name)}">{DataLayout.Esc(p.Name)} <span style="color:#555">({DataLayout.Esc(p.TypeName)})</span></label>""");
                        body.AppendLine($"""<input id="{formId}-{DataLayout.Esc(p.Name)}" name="{DataLayout.Esc(p.Name)}" type="{InputType(p.TypeName)}" placeholder="{DataLayout.Esc(p.TypeName)}" required>""");
                    }

                    body.AppendLine("""<button type="submit">Anropa</button>""");
                    body.AppendLine($"""<div class="result-banner" id="{formId}-result"></div>""");
                    body.AppendLine("</form>");
                    body.AppendLine("</div>");
                }

                body.AppendLine("</section>");
            }
        }

        body.AppendLine(MethodFormScript());

        return DataLayout.Wrap(
            $"{typeName} — {idString[..Math.Min(8, idString.Length)]}...",
            $"""<a href="/">Hem</a> / <a href="/data">Data</a> / <a href="/data/{DataLayout.Esc(typeName)}">{DataLayout.Esc(typeName)}</a> / <strong>{DataLayout.Esc(idString[..Math.Min(8, idString.Length)])}...</strong>""",
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
                        setTimeout(() => location.reload(), 1200);
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
        DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss zzz"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        _ => val.ToString() ?? "–"
    };

    private static string FormatTypeName(Type t)
    {
        if (t == typeof(string)) return "string";
        if (t == typeof(int)) return "int";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(Guid)) return "Guid";
        if (t == typeof(byte[])) return "byte[]";
        if (t == typeof(DateTimeOffset)) return "DateTimeOffset";
        if (t == typeof(DateOnly)) return "DateOnly";

        var nullable = Nullable.GetUnderlyingType(t);
        if (nullable is not null) return FormatTypeName(nullable) + "?";

        return t.Name;
    }

    private static object? GetId(object entity)
    {
        var idProp = entity.GetType().GetProperty("Id");
        return idProp?.GetValue(entity);
    }

    private static string GetIdGuid(object entity)
    {
        var idVal = GetId(entity);
        if (idVal is null) return "";
        var valueProp = idVal.GetType().GetProperty("Value");
        if (valueProp is not null && valueProp.PropertyType == typeof(Guid))
            return ((Guid)valueProp.GetValue(idVal)!).ToString();
        return idVal.ToString() ?? "";
    }
}
