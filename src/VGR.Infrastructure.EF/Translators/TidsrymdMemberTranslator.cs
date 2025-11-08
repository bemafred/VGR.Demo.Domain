using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using VGR.Domain;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdMemberTranslator : IMemberTranslator
{
    private readonly ISqlExpressionFactory _sql;
    public TidsrymdMemberTranslator(ISqlExpressionFactory sql) => _sql = sql;

    public SqlExpression? Translate(
        SqlExpression instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null) return null;
        if (member.DeclaringType != typeof(Tidsrymd)) return null;

        if (member.Name == nameof(Tidsrymd.ÄrTillsvidare))
        {
            var slut = _sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));
            return _sql.IsNull(slut);
        }

        return null;
    }
}
