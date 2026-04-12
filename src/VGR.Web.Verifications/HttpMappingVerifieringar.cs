using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Technical.Web.Mapping;
using Xunit;

namespace VGR.Web.Verifications;

public class HttpMappingVerifieringar
{
    public static IEnumerable<object[]> DomänfelScenarier()
    {
        yield return [new DomainArgumentFormatException("Test.Kod", "msg"), 400, "argument-format"];
        yield return [new DomainValidationException("Test.Kod", "msg"), 422, "validation"];
        yield return [new DomainAggregateNotFoundException("Test.Kod", "msg"), 404, "aggregate-not-found"];
        yield return [new DomainInvariantViolationException("Test.Kod", "msg"), 409, "invariant-violation"];
        yield return [new DomainInvalidStateTransitionException("Test.Kod", "msg"), 409, "invalid-state-transition"];
        yield return [new DomainConcurrencyConflictException("Test.Kod", "msg"), 409, "concurrency-conflict"];
        yield return [new DomainIdempotencyViolationException("Test.Kod", "msg"), 409, "idempotency-violation"];
        yield return [new DomainUndefinedOperationException("Test.Kod", "msg"), 422, "undefined-operation"];
    }

    [Theory]
    [MemberData(nameof(DomänfelScenarier))]
    public void HandleException_DomänfelGerKorrektStatuskod(DomainException ex, int förväntatStatus, string förväntatTypeSlug)
    {
        var result = DomainMappingExtensions.HandleException(ex);

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(förväntatStatus);
        problem.ProblemDetails.Type.Should().Be($"urn:vgr:domain:{förväntatTypeSlug}");
    }

    [Theory]
    [MemberData(nameof(DomänfelScenarier))]
    public void HandleException_DomänfelInnehållerCode(DomainException ex, int förväntatStatus, string förväntatTypeSlug)
    {
        var result = DomainMappingExtensions.HandleException(ex);

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.ProblemDetails.Extensions.Should().ContainKey("code")
            .WhoseValue.Should().Be(ex.Code);
    }

    [Fact]
    public void HandleException_DbUpdateConcurrencyException_Ger409()
    {
        var result = DomainMappingExtensions.HandleException(new DbUpdateConcurrencyException());

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(409);
        problem.ProblemDetails.Type.Should().Be("urn:vgr:infrastructure:concurrency-conflict");
    }

    [Fact]
    public void HandleException_DbUpdateException_Ger422()
    {
        var result = DomainMappingExtensions.HandleException(new DbUpdateException());

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(422);
        problem.ProblemDetails.Type.Should().Be("urn:vgr:infrastructure:constraint-violation");
    }

    [Fact]
    public void HandleException_GenerisktFel_Ger500()
    {
        var result = DomainMappingExtensions.HandleException(new InvalidOperationException("oväntat"));

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(500);
        problem.ProblemDetails.Type.Should().Be("urn:vgr:infrastructure:internal-error");
    }

    [Fact]
    public void HandleException_UnwrapparTargetInvocationException()
    {
        var inner = new DomainValidationException("Test.Kod", "msg");
        var wrapper = new TargetInvocationException(inner);

        var result = DomainMappingExtensions.HandleException(wrapper);

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(422);
        problem.ProblemDetails.Extensions["code"].Should().Be("Test.Kod");
    }
}
