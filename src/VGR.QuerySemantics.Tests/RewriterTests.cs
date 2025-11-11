using System.Linq.Expressions;
using FluentAssertions;
using Xunit;


namespace VGR.QuerySemantics.Tests;

public class RewriterTests
{
    [Fact]
    public void Rewrites_domain_method_call_to_plain_expression()
    {
        var sem = new VGR.QuerySemantics.QuerySemantics()
            .Register<SamplePeriod, DateTimeOffset, bool>(
                (p, t) => p.Contains(t),
                (p, t) => p.Start <= t && (p.End == null || t < p.End));

        Expression<Func<SamplePeriod, bool>> dom = x => x.Contains(DateTimeOffset.UnixEpoch);
        var rewritten = sem.Rewrite(dom);

        var body = ((LambdaExpression)rewritten).Body;
        body.ToString().Should().NotContain("Contains");
        body.Should().BeAssignableTo<BinaryExpression>();
    }
}
