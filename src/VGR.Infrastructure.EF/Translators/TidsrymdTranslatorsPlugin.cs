using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;

namespace VGR.Infrastructure.EF.Translators;

internal sealed class TidsrymdTranslatorsPlugin :
    IMemberTranslatorPlugin,
    IMethodCallTranslatorPlugin
{
    public TidsrymdTranslatorsPlugin(ISqlExpressionFactory sql)
    {
        MemberTranslators = new IMemberTranslator[] { new TidsrymdMemberTranslator(sql) };
        MethodCallTranslators = new IMethodCallTranslator[] { new TidsrymdMethodTranslator(sql) };
    }

    public IEnumerable<IMemberTranslator> MemberTranslators { get; }
    public IEnumerable<IMethodCallTranslator> MethodCallTranslators { get; }
}
