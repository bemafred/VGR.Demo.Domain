using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

using VGR.Domain.SharedKernel;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdMemberTranslator(ISqlExpressionFactory sql) : IMemberTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null) return null;
        if (member.DeclaringType != typeof(Tidsrymd)) return null;

        if (member.Name == nameof(Tidsrymd.ÄrTillsvidare))
        {
            var slut = sql.Property(instance, typeof(DateTimeOffset?), nameof(Tidsrymd.Slut));
            return sql.IsNull(slut);
        }

        return null;
    }
}
