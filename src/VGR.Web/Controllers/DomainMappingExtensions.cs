using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Technical;

namespace VGR.Web.Controllers;

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
            return HandleExceptions(self, ex, logger);
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
            return HandleExceptions(self, ex, logger);
        }
    }

    private static IActionResult ToHttp(ControllerBase self, Utfall utfall)
        => utfall.IsSuccess
            ? self.Ok()
            : BusinessProblem(self, utfall.Error, utfall.Code);

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

    private static IActionResult HandleExceptions(ControllerBase self, Exception ex, ILogger logger)
    {
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

            case DomainArgumentFormatException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "argument-format", 400);

            case DomainValidationException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "validation", 422);

            case DomainAggregateNotFoundException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "aggregate-not-found", 404);

            case DomainInvariantViolationException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "invariant-violation", 409);

            case DomainInvalidStateTransitionException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "invalid-state-transition", 409);

            case DomainConcurrencyConflictException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "concurrency-conflict", 409);

            case DomainIdempotencyViolationException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "idempotency-violation", 409);

            case DomainUndefinedOperationException e:
                logger.LogWarning("Domänfel {Code}: {Message}", e.Code, e.Message);
                return DomainProblem(self, e, "undefined-operation", 422);

            default:
                logger.LogError(ex, "Ohanterat fel.");
                return InfrastructureProblem(self, "urn:vgr:infrastructure:internal-error",
                    "Internt fel", 500,
                    "Ett oväntat fel inträffade.");
        }
    }

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
