using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Renderar <c>GET /data/{type}/{id}/{relation}</c> — visar relaterade entiteter.
/// </summary>
internal static class DataRelationPage
{
    public static string Render(string typeName, string idString, string relation, object entity, Type clrType, DbContext ctx)
    {
        var body = new StringBuilder();

        var navProp = clrType.GetProperty(relation, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (navProp is null)
        {
            body.AppendLine($"""<p class="empty">Relationen '{DataLayout.Esc(relation)}' hittades inte på {DataLayout.Esc(typeName)}.</p>""");
            return Wrap(typeName, idString, relation, body);
        }

        var navVal = navProp.GetValue(entity);

        // Collection-navigation
        if (navVal is IEnumerable collection && navProp.PropertyType != typeof(string) && navProp.PropertyType != typeof(byte[]))
        {
            var elementType = navProp.PropertyType.IsGenericType
                ? navProp.PropertyType.GetGenericArguments()[0]
                : typeof(object);

            var items = collection.Cast<object>().ToList();

            // Identifiera kolumner: skalärproperties
            var props = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !typeof(IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string) || p.PropertyType == typeof(byte[]))
                .Where(p => p.Name != "RowVersion")
                .ToList();

            body.AppendLine($"<p>{items.Count} relaterade {DataLayout.Esc(elementType.Name)}</p>");
            body.AppendLine("<table>");
            body.AppendLine("<thead><tr>");
            foreach (var p in props)
                body.AppendLine($"<th>{DataLayout.Esc(p.Name)}</th>");
            body.AppendLine("</tr></thead>");
            body.AppendLine("<tbody>");

            foreach (var item in items)
            {
                body.AppendLine("<tr>");
                foreach (var p in props)
                {
                    var val = p.GetValue(item);
                    var display = FormatValue(val);

                    if (p.Name == "Id")
                    {
                        var idGuid = ExtractGuid(val);
                        body.AppendLine($"""<td class="mono"><a class="entity-link" href="/data/{DataLayout.Esc(elementType.Name)}/{idGuid}">{DataLayout.Esc(display)}</a></td>""");
                    }
                    else
                        body.AppendLine($"""<td class="mono">{DataLayout.Esc(display)}</td>""");
                }
                body.AppendLine("</tr>");
            }

            body.AppendLine("</tbody></table>");

            if (items.Count == 0)
                body.AppendLine("""<p class="empty">Inga relaterade entiteter.</p>""");
        }
        else
        {
            // Reference-navigation — visa enstaka relaterad entitet
            if (navVal is null)
            {
                body.AppendLine("""<p class="empty">Ingen relaterad entitet.</p>""");
            }
            else
            {
                var targetId = ExtractGuid(navVal.GetType().GetProperty("Id")?.GetValue(navVal));
                var targetType = navVal.GetType().Name;
                body.AppendLine($"""<p>Refererar till <a class="entity-link" href="/data/{DataLayout.Esc(targetType)}/{targetId}">{DataLayout.Esc(targetType)} {DataLayout.Esc(targetId[..Math.Min(8, targetId.Length)])}...</a></p>""");
            }
        }

        return Wrap(typeName, idString, relation, body);
    }

    private static string Wrap(string typeName, string idString, string relation, StringBuilder body)
    {
        var shortId = idString[..Math.Min(8, idString.Length)];
        return DataLayout.Wrap(
            $"{typeName} — {relation}",
            $"""<a href="/">Hem</a> / <a href="/data">Data</a> / <a href="/data/{DataLayout.Esc(typeName)}">{DataLayout.Esc(typeName)}</a> / <a href="/data/{DataLayout.Esc(typeName)}/{DataLayout.Esc(idString)}">{DataLayout.Esc(shortId)}...</a> / <strong>{DataLayout.Esc(relation)}</strong>""",
            body.ToString());
    }

    private static string FormatValue(object? val) => val switch
    {
        null => "–",
        byte[] => "[bytes]",
        DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        _ => val.ToString() ?? "–"
    };

    private static string ExtractGuid(object? val)
    {
        if (val is null) return "";
        var valueProp = val.GetType().GetProperty("Value");
        if (valueProp is not null && valueProp.PropertyType == typeof(Guid))
            return ((Guid)valueProp.GetValue(val)!).ToString();
        return val.ToString() ?? "";
    }
}
