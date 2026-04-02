
using Microsoft.AspNetCore.Mvc;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Technical;

namespace VGR.Web.Controllers;

public static class DomainMappingExtensions
{
    public static async Task<IActionResult> Map(this ControllerBase self, Func<CancellationToken, Task<Utfall>> action, CancellationToken ct)
    {
        try
        {
            var outcome = await action(ct);
            return ToHttp(self, outcome);
        }
        catch (Exception ex)
        {
            return HandleExceptions(self, ex);
        }
    }

    public static async Task<IActionResult> Map<T>(this ControllerBase self, Func<CancellationToken, Task<Utfall<T>>> action, CancellationToken ct, Func<T, object>? shape = null)
    {
        try
        {
            var outcome = await action(ct);
            return ToHttp(self, outcome, shape);
        }
        catch (Exception ex)
        {
            return HandleExceptions(self, ex);
        }
    }

    private static IActionResult ToHttp(ControllerBase self, Utfall utfall)
        => utfall.IsSuccess ? self.Ok() : Problem(self, utfall.Error);

    private static IActionResult ToHttp<T>(ControllerBase self, Utfall<T> outcome, Func<T, object>? shape = null)
        => outcome.IsSuccess
            ? self.Ok(shape is null ? (object?)outcome.Value! : shape(outcome.Value!))
            : Problem(self, outcome.Error);

    private static IActionResult Problem(ControllerBase self, string? msg)
        => self.Problem(detail: msg, statusCode: 400);

    private static IActionResult HandleExceptions(ControllerBase self, Exception ex)
        => ex switch
        {
            DomainArgumentFormatException e        => DomainProblem(self, e, 400),
            DomainValidationException e            => DomainProblem(self, e, 422),
            DomainAggregateNotFoundException e     => DomainProblem(self, e, 404),
            DomainInvariantViolationException e    => DomainProblem(self, e, 409),
            DomainInvalidStateTransitionException e => DomainProblem(self, e, 409),
            DomainConcurrencyConflictException e   => DomainProblem(self, e, 409),
            DomainIdempotencyViolationException e  => DomainProblem(self, e, 409),
            _ => self.Problem("Internt fel", statusCode: 500)
        };

    private static IActionResult DomainProblem(ControllerBase self, DomainException ex, int statusCode)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Detail = ex.Message,
            Extensions = { ["code"] = ex.Code }
        };
        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
