
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
            DomainInvariantViolationException e => self.Problem(e.Message, statusCode: 409),
            DomainArgumentFormatException e => self.Problem($"{e.Code}: {e.Message}", statusCode: 400),
            _ => self.Problem("Internt fel", statusCode: 500)
        };
}
