using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using VGR.Domain;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdMethodTranslator : IMethodCallTranslator
{
    private readonly ISqlExpressionFactory _sql;

    private static readonly MethodInfo OverlapsMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.Överlappar))!;

    private static readonly MethodInfo ContainsInstantMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.Innehåller), new[] { typeof(DateTimeOffset) })!;

    private static readonly MethodInfo LongerThanMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.VararLängreÄn), new[] { typeof(TimeSpan) })!;

    private static readonly SqlConstantExpression MaxDateTimeOffset =
        new SqlConstantExpression(
            System.Linq.Expressions.Expression.Constant(
                new DateTimeOffset(9999, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            typeMapping: null);

    public TidsrymdMethodTranslator(ISqlExpressionFactory sql) => _sql = sql;

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
            var aStart = _sql.Property(instance, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var aSlut  = _sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));

            var b      = arguments[0];
            var bStart = _sql.Property(b, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var bSlut  = _sql.Property(b, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));

            var aEnd = _sql.Coalesce(aSlut, MaxDateTimeOffset);
            var bEnd = _sql.Coalesce(bSlut, MaxDateTimeOffset);

            return _sql.AndAlso(
                _sql.GreaterThan(aEnd, bStart),
                _sql.GreaterThan(bEnd, aStart));
        }

        if (method == ContainsInstantMethod)
        {
            // Innehåller(t) => Start <= t AND COALESCE(Slut, Max) > t    (half-open [Start, Slut))
            var start = _sql.Property(instance, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var slut  = _sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));
            var end   = _sql.Coalesce(slut, MaxDateTimeOffset);
            var t     = arguments[0];

            return _sql.AndAlso(
                _sql.LessThanOrEqual(start, t),
                _sql.GreaterThan(end, t));
        }

        if (method == LongerThanMethod)
        {
            // VararLängreÄn(d) => (COALESCE(Slut, Max) - Start) > d
            // EF doesn't support direct DateTimeOffset subtraction to TimeSpan in SQL for all providers,
            // so compute as end > Start + d
            var start = _sql.Property(instance, typeof(DateTimeOffset), nameof(Tidsrymd.Start));
            var slut  = _sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));
            var end   = _sql.Coalesce(slut, MaxDateTimeOffset);
            var span  = arguments[0]; // TimeSpan parameter

            // build Start + d using DateAdd if available via function - provider-specific.
            // Fallback: compare (end - start) > d via provider function 'DATEDIFF_BIG' is provider-specific too.
            // Generic approach: end > EF.Functions.DateAdd(start, d) is not available by default.
            // We'll translate to: (julianday(end) - julianday(start)) * 86400000.0 > d.TotalMilliseconds for Sqlite
            // but since this is provider-specific, keep a generic expression that most providers can optimize:
            // end > start + d  ==> use SQL function DATEADD via SqlFunctionExpression "DATEADD" when available.

            // Try generic: end > DATEADD(millisecond, d.TotalMilliseconds, start)
            // Build DATEADD call: DATEADD(millisecond, milliseconds, start)
            var milliseconds = _sql.Convert(span, typeof(double)); // span.TotalMilliseconds requires provider support
            var dateAdd = _sql.Function(
                "DATEADD",
                new SqlExpression[] {
                    _sql.Fragment("millisecond"),
                    _sql.Convert(milliseconds, typeof(int)), // truncation is acceptable for typical comparisons
                    start
                },
                nullable: true,
                argumentsPropagateNullability: new[] { false, true, true },
                typeof(DateTimeOffset));

            return _sql.GreaterThan(end, dateAdd);
        }

        return null;
    }
}
