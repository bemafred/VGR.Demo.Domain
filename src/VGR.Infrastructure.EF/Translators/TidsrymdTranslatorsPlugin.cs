using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdTranslatorsPlugin(ISqlExpressionFactory sqlExpressionFactory)
    : IMemberTranslatorPlugin, IMethodCallTranslatorPlugin
{
    public IEnumerable<IMemberTranslator> Translators { get; } =
    [
        new TidsrymdMemberTranslator(sqlExpressionFactory)
    ];

    IEnumerable<IMethodCallTranslator> IMethodCallTranslatorPlugin.Translators { get; } =
    [
        new TidsrymdMethodTranslator(sqlExpressionFactory)
    ];
}
