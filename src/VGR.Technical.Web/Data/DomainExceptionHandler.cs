using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace VGR.Technical.Web.Data;

/// <summary>
/// Reflection-baserad exception → IResult-mappning.
/// Speglar DomainMappingExtensions.HandleExceptions men utan kompileringstidsberoende till VGR.Domain.
/// </summary>
internal static class DomainExceptionHandler
{
    private static readonly Dictionary<string, (int Status, string TypeSlug)> ExceptionMap = new()
    {
        ["DomainArgumentFormatException"] = (400, "argument-format"),
        ["DomainValidationException"] = (422, "validation"),
        ["DomainAggregateNotFoundException"] = (404, "aggregate-not-found"),
        ["DomainInvariantViolationException"] = (409, "invariant-violation"),
        ["DomainInvalidStateTransitionException"] = (409, "invalid-state-transition"),
        ["DomainConcurrencyConflictException"] = (409, "concurrency-conflict"),
        ["DomainIdempotencyViolationException"] = (409, "idempotency-violation"),
        ["DomainUndefinedOperationException"] = (422, "undefined-operation"),
    };

    public static IResult HandleException(Exception ex)
    {
        // Unwrap TargetInvocationException från reflection-anrop
        if (ex is TargetInvocationException { InnerException: { } inner })
            ex = inner;

        // Kolla om undantaget ärver DomainException (via typnamn i hierarkin)
        var exType = ex.GetType();
        if (IsDomainException(exType))
        {
            var typeName = exType.Name;
            var code = exType.GetProperty("Code")?.GetValue(ex)?.ToString();

            if (ExceptionMap.TryGetValue(typeName, out var mapping))
            {
                return Results.Problem(
                    type: $"urn:vgr:domain:{mapping.TypeSlug}",
                    title: typeName.Replace("Domain", "").Replace("Exception", ""),
                    statusCode: mapping.Status,
                    detail: ex.Message,
                    extensions: code is not null ? new Dictionary<string, object?> { ["code"] = code } : null);
            }
        }

        // Okänt domänfel eller infrastrukturfel
        return Results.Problem(
            type: "urn:vgr:infrastructure:internal-error",
            title: "Internt fel",
            statusCode: 500,
            detail: ex.Message);
    }

    private static bool IsDomainException(Type? type)
    {
        while (type is not null && type != typeof(Exception))
        {
            if (type.Name == "DomainException") return true;
            type = type.BaseType;
        }
        return false;
    }
}
