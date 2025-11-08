using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdTranslatorsPlugin(ISqlExpressionFactory sqlExpressionFactory, IModel model)
    : IMemberTranslatorPlugin, IMethodCallTranslatorPlugin
{
    public IEnumerable<IMemberTranslator> Translators { get; } =
    [
        new TidsrymdMemberTranslator(sqlExpressionFactory, model)
    ];

    IEnumerable<IMethodCallTranslator> IMethodCallTranslatorPlugin.Translators { get; } =
    [
        new TidsrymdMethodTranslator(sqlExpressionFactory, model)
    ];
}
