using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VGR.Application.Vårdval;
using VGR.Domain.SharedKernel;
using VGR.Technical.Web.Mapping;

namespace VGR.Web.Controllers;

/// <summary>API-yta för vårdvalsoperationer.</summary>
[ApiController]
[Route("api/personer/{personId:guid}/vardval")]
public sealed class VårdvalController(SkapaVårdvalInteractor interactor) : ControllerBase
{
    /// <summary>Indata för att skapa ett vårdval.</summary>
    public sealed record SkapaVårdvalDto(
        [Required, StringLength(64, MinimumLength = 1)] string EnhetsHsaId,
        DateOnly Start,
        DateOnly? Slut);

    /// <summary>Skapar ett nytt vårdval för angiven person.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Skapa(Guid personId, [FromBody] SkapaVårdvalDto body, CancellationToken ct)
    {
        if (personId == Guid.Empty)
            return Problem(detail: "PersonId får inte vara tomt.", statusCode: 400);

        var command = new SkapaVårdvalCmd(new(personId), body.EnhetsHsaId, body.Start, body.Slut);
        return await this.Map(cancellationToken => interactor.ProcessAsync(command, cancellationToken), ct);
    }
}
