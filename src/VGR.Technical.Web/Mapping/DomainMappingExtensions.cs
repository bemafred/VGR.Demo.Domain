using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Technical;

namespace VGR.Technical.Web.Mapping;

/// <summary>
/// Extensionmetoder för att mappa interaktorresultat (<see cref="Utfall"/>) och domänundantag till HTTP-svar.
/// Hanterar båda felkanalerna (ADR-007) och översätter till RFC 9457-kompatibla <see cref="ProblemDetails"/>.
/// </summary>
public static class DomainMappingExtensions
{
    /// <summary>Kör en interaktor som returnerar <see cref="Utfall"/> och mappar resultatet till HTTP.</summary>
    public static async Task<IActionResult> Map(this ControllerBase self, Func<CancellationToken, Task<Utfall>> action, CancellationToken ct)
    {
        var logger = self.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("VGR.Web.Map");

        try
        {
            var outcome = await action(ct);
            return ToHttp(self, outcome);
        }
        catch (Exception ex)
        {
            return HandleExceptionAsAction(self, ex, logger);
        }
    }

    /// <summary>Kör en interaktor som returnerar <see cref="Utfall{T}"/> och mappar resultatet till HTTP. Valfri <paramref name="shape"/> projicerar svaret.</summary>
    public static async Task<IActionResult> Map<T>(this ControllerBase self, Func<CancellationToken, Task<Utfall<T>>> action, CancellationToken ct, Func<T, object>? shape = null)
    {
        var logger = self.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("VGR.Web.Map");

        try
        {
            var outcome = await action(ct);
            return ToHttp(self, outcome, shape);
        }
        catch (Exception ex)
        {
            return HandleExceptionAsAction(self, ex, logger);
        }
    }

    /// <summary>Mappar ett undantag till <see cref="IResult"/> för minimal API-endpoints.</summary>
    public static IResult HandleException(Exception ex)
    {
        ex = Unwrap(ex);

        if (ex is DomainException domainEx)
        {
            var (statusCode, typeSlug) = Classify(domainEx);
            return Results.Problem(
                type: $"urn:vgr:domain:{typeSlug}",
                title: domainEx.GetType().Name.Replace("Domain", "").Replace("Exception", ""),
                statusCode: statusCode,
                detail: domainEx.Message,
                extensions: new Dictionary<string, object?> { ["code"] = domainEx.Code });
        }

        if (ex is DbUpdateConcurrencyException)
        {
            return Results.Problem(
                type: "urn:vgr:infrastructure:concurrency-conflict",
                title: "Samtidighetskonflikt",
                statusCode: 409,
                detail: "Resursen har ändrats av en annan begäran. Försök igen.");
        }

        if (ex is DbUpdateException)
        {
            return Results.Problem(
                type: "urn:vgr:infrastructure:constraint-violation",
                title: "Databaskonflikt",
                statusCode: 422,
                detail: "Begäran bryter mot en databaskonstraint.");
        }

        return Results.Problem(
            type: "urn:vgr:infrastructure:internal-error",
            title: "Internt fel",
            statusCode: 500,
            detail: "Ett oväntat fel inträffade.");
    }

    private static Exception Unwrap(Exception ex)
        => ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

    private static IActionResult ToHttp(ControllerBase self, Utfall utfall)
        => utfall.IsSuccess ? self.Ok() : BusinessProblem(self, utfall.Error, utfall.Code);

    private static IActionResult ToHttp<T>(ControllerBase self, Utfall<T> outcome, Func<T, object>? shape = null)
        => outcome.IsSuccess
            ? self.Ok(shape is null ? (object?)outcome.Value! : shape(outcome.Value!))
            : BusinessProblem(self, outcome.Error, outcome.Code);

    private static IActionResult BusinessProblem(ControllerBase self, string? msg, string? code)
    {
        var problem = new ProblemDetails
        {
            Type = "urn:vgr:application:business-failure",
            Title = "Affärsfel",
            Status = 400,
            Detail = msg
        };

        if (code is not null)
            problem.Extensions["code"] = code;

        return new ObjectResult(problem) { StatusCode = 400 };
    }

    private static IActionResult HandleExceptionAsAction(ControllerBase self, Exception ex, ILogger logger)
    {
        ex = Unwrap(ex);

        switch (ex)
        {
            case OperationCanceledException:
                logger.LogInformation("Begäran avbröts av klienten.");
                return new StatusCodeResult(499);

            case DbUpdateConcurrencyException concurrency:
                logger.LogWarning(concurrency, "Optimistisk samtidighetskonflikt.");
                return InfrastructureProblem(self, "urn:vgr:infrastructure:concurrency-conflict",
                    "Samtidighetskonflikt", 409,
                    "Resursen har ändrats av en annan begäran. Försök igen.");

            case DbUpdateException dbUpdate:
                logger.LogWarning(dbUpdate, "Databaskonstraint bruten.");
                return InfrastructureProblem(self, "urn:vgr:infrastructure:constraint-violation",
                    "Databaskonflikt", 422,
                    "Begäran bryter mot en databaskonstraint.");

            case DomainException domainEx:
                logger.LogWarning("Domänfel {Code}: {Message}", domainEx.Code, domainEx.Message);
                var (statusCode, typeSlug) = Classify(domainEx);
                return DomainProblem(self, domainEx, typeSlug, statusCode);

            default:
                logger.LogError(ex, "Ohanterat fel.");

                return InfrastructureProblem(self, "urn:vgr:infrastructure:internal-error",
                    "Internt fel", 500,
                    "Ett oväntat fel inträffade.");
        }
    }

    private static (int StatusCode, string TypeSlug) Classify(DomainException ex) => ex switch
    {
        DomainArgumentFormatException       => (400, "argument-format"),
        DomainValidationException           => (422, "validation"),
        DomainAggregateNotFoundException    => (404, "aggregate-not-found"),
        DomainInvariantViolationException   => (409, "invariant-violation"),
        DomainInvalidStateTransitionException => (409, "invalid-state-transition"),
        DomainConcurrencyConflictException  => (409, "concurrency-conflict"),
        DomainIdempotencyViolationException => (409, "idempotency-violation"),
        DomainUndefinedOperationException   => (422, "undefined-operation"),
        _                                   => (500, "unknown")
    };

    private static IActionResult DomainProblem(ControllerBase self, DomainException ex, string typeSlug, int statusCode)
    {
        var problem = new ProblemDetails
        {
            Type = $"urn:vgr:domain:{typeSlug}",
            Title = ex.GetType().Name.Replace("Domain", "").Replace("Exception", ""),
            Status = statusCode,
            Detail = ex.Message,
            Extensions = { ["code"] = ex.Code }
        };

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    private static IActionResult InfrastructureProblem(ControllerBase self, string type, string title, int statusCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail
        };

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
