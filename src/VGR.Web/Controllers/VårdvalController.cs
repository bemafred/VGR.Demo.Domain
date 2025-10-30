
using Microsoft.AspNetCore.Mvc;
using VGR.Application.Vårdval;
using VGR.Domain.SharedKernel;
using VGR.Web.Controllers;

namespace VGR.Web.Controllers;

[ApiController]
[Route("api/personer/{personId:guid}/vardval")]
public sealed class VårdvalController(SkapaVårdvalInteractor interactor) : ControllerBase
{
    public sealed record SkapaVårdvalDto(string EnhetsHsaId, string LäkaresHsaId, DateOnly Start, DateOnly? Slut);

    [HttpPost]
    public async Task<IActionResult> Skapa(Guid personId, [FromBody] SkapaVårdvalDto body, CancellationToken ct)
    {
        var command = new SkapaVårdvalCmd(new(personId), body.EnhetsHsaId, body.LäkaresHsaId, body.Start, body.Slut);
        return await this.Map(cancellationToken => interactor.ProcessAsync(command, cancellationToken), ct);
    }
}
