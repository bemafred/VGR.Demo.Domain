using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Infrastructure.EF;
using VGR.Technical;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Application.Vårdval;

public sealed record SkapaVårdvalCmd(PersonId PersonId, string EnhetsHsaId, string LäkaresHsaId, DateOnly Start, DateOnly? Slut);

public sealed class SkapaVårdvalInteractor(ReadDbContext read, WriteDbContext write, IClock clock)
{
    public async Task<Outcome<VardvalId>> ProcessAsync(SkapaVårdvalCmd cmd, CancellationToken ct)
    {
        var enhet = HsaId.Parse(cmd.EnhetsHsaId);
        var läkare = HsaId.Parse(cmd.LäkaresHsaId);
        var giltighet = Tidsrymd.Skapa(cmd.Start, cmd.Slut);

        var kandidater = await read.Vardval
            .Where(v => v.PersonId == cmd.PersonId && v.EnhetsHsaId == enhet)
            .ToListAsync(ct);

        var överlapp = kandidater.Any(v => v.Giltighet.Överlappar(giltighet));

        if (överlapp)
            return Outcome<VardvalId>.Fail($"Överlappande vårdval för enhet {enhet} under {giltighet}.");

        var person = await write.Personer.FirstOrDefaultAsync(p => p.Id == cmd.PersonId, ct);

        if (person == null)
            Throw.Person.Saknas(cmd.PersonId);

        await write.Entry(person).Collection(p => p.AllaVårdval).Query()
            .Where(v => v.EnhetsHsaId == enhet)
            .LoadAsync(ct);

        var vårdval = person.SkapaVårdval(enhet, giltighet, clock.UtcNow);

        await write.SaveChangesAsync(ct);

        return Outcome<VardvalId>.Ok(vårdval.Id);
    }
}
