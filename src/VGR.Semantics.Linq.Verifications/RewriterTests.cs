using System.Linq.Expressions;
using FluentAssertions;
using Xunit;

using VGR.Semantics.Linq;

namespace VGR.Semantics.Linq.Verifications;

public class RewriterTests
{
    [Fact]
    public void Rewrites_domain_method_call_to_plain_expression()
    {
        SemanticRegistry.Register<SamplePeriod, DateTimeOffset, bool>(
            (p, t) => p.Contains(t),
            (p, t) => p.Start <= t && (p.End == null || t < p.End));

        Expression<Func<SamplePeriod, bool>> dom = x => x.Contains(DateTimeOffset.UnixEpoch);
        var rewritten = SemanticRegistry.Rewrite(dom);

        var body = ((LambdaExpression)rewritten).Body;
        body.ToString().Should().NotContain("Contains");
        body.Should().BeAssignableTo<BinaryExpression>();
    }
}
