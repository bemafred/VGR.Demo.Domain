using System.Text;
using VGR.Semantics.Abstractions;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Renderar <c>GET /data</c> — listar alla aggregattyper med länk till instanslistan.
/// </summary>
internal static class DataListPage
{
    public static string Render(DomainModel model)
    {
        var aggregates = model.Types.Where(t => t.Kind == DomainTypeKind.Aggregate).OrderBy(t => t.Name).ToList();
        var entities = model.Types.Where(t => t.Kind == DomainTypeKind.Entity).OrderBy(t => t.Name).ToList();
        var all = aggregates.Concat(entities).ToList();

        var body = new StringBuilder();

        body.AppendLine("<table>");
        body.AppendLine("<thead><tr><th>Typ</th><th>Klassificering</th><th>Egenskaper</th><th>Beteenden</th></tr></thead>");
        body.AppendLine("<tbody>");

        foreach (var type in all)
        {
            var kindCss = type.Kind.ToString().ToLowerInvariant();
            var kindLabel = type.Kind == DomainTypeKind.Aggregate ? "Aggregat" : "Entitet";
            body.AppendLine($"""<tr>""");
            body.AppendLine($"""  <td><a class="entity-link mono" href="/data/{DataLayout.Esc(type.Name)}">{DataLayout.Esc(type.Name)}</a></td>""");
            body.AppendLine($"""  <td><span class="badge {kindCss}">{kindLabel}</span></td>""");
            body.AppendLine($"""  <td>{type.Properties.Count}</td>""");
            body.AppendLine($"""  <td>{type.Methods.Count}</td>""");
            body.AppendLine("</tr>");
        }

        body.AppendLine("</tbody></table>");

        if (all.Count == 0)
            body.AppendLine("""<p class="empty">Inga aggregat eller entiteter hittades.</p>""");

        return DataLayout.Wrap(
            "Data",
            """<a href="/">Hem</a> / <strong>Data</strong>""",
            body.ToString());
    }
}
