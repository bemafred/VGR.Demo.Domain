using Microsoft.AspNetCore.Mvc;
using VGR.Application.Personer;
using VGR.Domain.SharedKernel;
using VGR.Web.Controllers;

namespace VGR.Web.Controllers;

/// <summary>API-yta för personrelaterade operationer inom en region.</summary>
[ApiController]
[Route("api/regioner/{regionId:guid}/personer")]
public sealed class PersonerController(SkapaPersonInteractor interactor) : ControllerBase
{
    /// <summary>Skapar en ny person i angiven region.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Skapa(Guid regionId, [FromBody] SkapaPersonDto body, CancellationToken ct)
    {
        if (regionId == Guid.Empty)
            return Problem(detail: "RegionId får inte vara tomt.", statusCode: 400);

        var command = new SkapaPersonCmd(new(regionId), body.Personnummer);
        return await this.Map(cancellationToken => interactor.ProcessAsync(command, cancellationToken), ct);
    }
}
