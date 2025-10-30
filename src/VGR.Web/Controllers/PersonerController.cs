
using Microsoft.AspNetCore.Mvc;
using VGR.Application.Personer;
using VGR.Domain.SharedKernel;
using VGR.Web.Controllers;

namespace VGR.Web.Controllers;

[ApiController]
[Route("api/regioner/{regionId:guid}/personer")]
public sealed class PersonerController(SkapaPersonInteractor interactor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Skapa(Guid regionId, [FromBody] SkapaPersonDto body, CancellationToken ct)
    {
        var command = new SkapaPersonCmd(new(regionId), body.Personnummer);
        return await this.Map(cancellationToken => interactor.ProcessAsync(command, cancellationToken), ct);
    }
}
