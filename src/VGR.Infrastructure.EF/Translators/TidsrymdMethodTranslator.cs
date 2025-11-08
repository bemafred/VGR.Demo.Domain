using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using VGR.Domain.SharedKernel;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdMethodTranslator : IMethodCallTranslator
{
    private readonly ISqlExpressionFactory _sql;
    private readonly IProperty? _startProperty;
    private readonly IProperty? _slutProperty;

    private static readonly MethodInfo OverlapsMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.Överlappar))!;

    private static readonly MethodInfo ContainsInstantMethod =
        typeof(Tidsrymd).GetMethod(nameof(Tidsrymd.Innehåller), [typeof(DateTimeOffset)])!;

    public TidsrymdMethodTranslator(ISqlExpressionFactory sql, IModel model)
    {
        _sql = sql;
        var complex = model.FindComplexType(typeof(Tidsrymd));
        _startProperty = complex?.FindProperty(nameof(Tidsrymd.Start));
        _slutProperty  = complex?.FindProperty(nameof(Tidsrymd.Slut));
    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null) return null;
        if (instance is not StructuralTypeReferenceExpression structural) return null;

        if (method.Equals(OverlapsMethod))
        {
            // v.Överlappar(other)
            if (_startProperty is null || _slutProperty is null) return null;

            var start = structural.BindProperty(_startProperty)!;
            var end   = structural.BindProperty(_slutProperty)!;

            var other = arguments[0];
            if (other is not StructuralTypeReferenceExpression otherStruct) return null;

            var otherStart = otherStruct.BindProperty(_startProperty)!;
            var otherEnd   = otherStruct.BindProperty(_slutProperty)!;

            // (start <= otherEnd) AND (otherStart <= end)  (hantera NULL som "tillsvidare": end IS NULL → +inf)
            var endOrMax = _sql.Coalesce(end, _sql.Constant(DateTimeOffset.MaxValue, end.TypeMapping));
            var otherEndOrMax = _sql.Coalesce(otherEnd, _sql.Constant(DateTimeOffset.MaxValue, otherEnd.TypeMapping));

            return _sql.And(
                _sql.LessThanOrEqual(start, otherEndOrMax),
                _sql.LessThanOrEqual(otherStart, endOrMax));
        }

        if (method.Equals(ContainsInstantMethod))
        {
            // v.Innehåller(instant)
            if (_startProperty is null || _slutProperty is null) return null;

            var start = structural.BindProperty(_startProperty)!;
            var end   = structural.BindProperty(_slutProperty)!;
            var instant = arguments[0];

            var endOrMax = _sql.Coalesce(end, _sql.Constant(DateTimeOffset.MaxValue, end.TypeMapping));

            return _sql.And(
                _sql.LessThanOrEqual(start, instant),
                _sql.GreaterThanOrEqual(endOrMax, instant));
        }

        return null;
    }
}
