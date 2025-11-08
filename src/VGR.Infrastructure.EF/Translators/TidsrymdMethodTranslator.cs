using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using VGR.Domain.SharedKernel;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdMethodTranslator(ISqlExpressionFactory sql) : IMethodCallTranslator
{
    private static readonly MethodInfo OverlapsMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.Överlappar))!;

    private static readonly MethodInfo ContainsInstantMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.Innehåller), [typeof(DateTimeOffset)])!;

    private static readonly MethodInfo LongerThanMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.VararLängreÄn), [typeof(TimeSpan)])!;

    private static readonly SqlConstantExpression MaxDateTimeOffset =
        new SqlConstantExpression(
            System.Linq.Expressions.Expression.Constant(
                new DateTimeOffset(9999, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            typeMapping: null);

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null) return null;

        if (method == OverlapsMethod)
        {
            // a = instance, b = arguments[0]
            var aStart = sql.Property(instance, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var aSlut  = sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));

            var b      = arguments[0];
            var bStart = sql.Property(b, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var bSlut  = sql.Property(b, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));

            var aEnd = sql.Coalesce(aSlut, MaxDateTimeOffset);
            var bEnd = sql.Coalesce(bSlut, MaxDateTimeOffset);

            return sql.AndAlso(
                sql.GreaterThan(aEnd, bStart),
                sql.GreaterThan(bEnd, aStart));
        }

        if (method == ContainsInstantMethod)
        {
            // Innehåller(t) => Start <= t AND COALESCE(Slut, Max) > t    (half-open [Start, Slut))
            var start = sql.Property(instance, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var slut  = sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));
            var end   = sql.Coalesce(slut, MaxDateTimeOffset);
            var t     = arguments[0];

            return sql.AndAlso(
                sql.LessThanOrEqual(start, t),
                sql.GreaterThan(end, t));
        }

        if (method == LongerThanMethod)
        {
            // VararLängreÄn(d) => (COALESCE(Slut, Max) - Start) > d
            // EF doesn't support direct DateTimeOffset subtraction to TimeSpan in SQL for all providers,
            // so compute as end > Start + d
            var start = sql.Property(instance, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var slut  = sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));
            var end   = sql.Coalesce(slut, MaxDateTimeOffset);
            var span  = arguments[0]; // TimeSpan parameter

            // build Start + d using DateAdd if available via function - provider-specific.
            // Fallback: compare (end - start) > d via provider function 'DATEDIFF_BIG' is provider-specific too.
            // Generic approach: end > EF.Functions.DateAdd(start, d) is not available by default.
            // We'll translate to: (julianday(end) - julianday(start)) * 86400000.0 > d.TotalMilliseconds for Sqlite
            // but since this is provider-specific, keep a generic expression that most providers can optimize:
            // end > start + d  ==> use SQL function DATEADD via SqlFunctionExpression "DATEADD" when available.

            // Try generic: end > DATEADD(millisecond, d.TotalMilliseconds, start)
            // Build DATEADD call: DATEADD(millisecond, milliseconds, start)
            var milliseconds = sql.Convert(span, typeof(double)); // span.TotalMilliseconds requires provider support
            var dateAdd = sql.Function(
                "DATEADD",
                new SqlExpression[] {
                    sql.Fragment("millisecond"),
                    sql.Convert(milliseconds, typeof(int)), // truncation is acceptable for typical comparisons
                    start
                },
                nullable: true,
                argumentsPropagateNullability: [false, true, true],
                typeof(DateTimeOffset));

            return sql.GreaterThan(end, dateAdd);
        }

        return null;
    }
}
