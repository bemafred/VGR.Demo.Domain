using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

using VGR.Domain.SharedKernel;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdMemberTranslator : IMemberTranslator
{
    private readonly ISqlExpressionFactory _sql;
    private readonly IProperty? _startProperty;
    private readonly IProperty? _slutProperty;

    public TidsrymdMemberTranslator(ISqlExpressionFactory sql, IModel model)
    {
        _sql = sql;
        // Hämta komplex-typens property-metadata en gång
        var complex = model.FindComplexType(typeof(Tidsrymd));
        _startProperty = complex?.FindProperty(nameof(Tidsrymd.Start));
        _slutProperty  = complex?.FindProperty(nameof(Tidsrymd.Slut));
    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null) return null;
        if (member.DeclaringType != typeof(Tidsrymd)) return null;

        // Vi förlitar oss på att 'instance' är en StructuralTypeReferenceExpression
        if (instance is not StructuralTypeReferenceExpression structural)
            return null;

        if (member.Name == nameof(Tidsrymd.ÄrTillsvidare))
        {
            if (_slutProperty is null) return null;
            var slut = structural.BindProperty(_slutProperty);
            return _sql.IsNull(slut);
        }

        return null;
    }
}
