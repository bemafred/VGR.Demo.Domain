using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Infrastructure.EF;
using VGR.Technical;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel.Exceptions;

namespace VGR.Application.Vårdval;

public sealed record SkapaVårdvalCmd(PersonId PersonId, string EnhetsHsaId, DateOnly Start, DateOnly? Slut);

public sealed class SkapaVårdvalInteractor(ReadDbContext read, WriteDbContext write, IClock clock)
{
    public async Task<Utfall<VårdvalId>> ProcessAsync(SkapaVårdvalCmd cmd, CancellationToken ct)
    {
        // 1) Validera indata
        var enhet = HsaId.Tolka(cmd.EnhetsHsaId);
        var giltighet = Tidsrymd.Skapa(cmd.Start, cmd.Slut);

        // 2) Ladda personen
        var person = await write.Personer
            .FirstOrDefaultAsync(p => p.Id == cmd.PersonId, ct);

        if (person is null)
            Throw.Person.Saknas(cmd.PersonId);

        // 3) Ladda endast det gällande, öppna vårdval som domänen behöver
        await write.Entry(person).Collection(p => p.AllaVårdval).Query()
            .Where(vårdval => vårdval.Period.Slut == null)
            .LoadAsync(ct);

        // 4) Skapa vårdvalet via domänen (stänger gällande, öppet vårdval)
        var vårdval = person.SkapaVårdval(enhet, giltighet, clock.UtcNow);

        //messageBus.EnqueueEvents(person.DequeueEvents());

        // 5) Uppdatera datalagrets tillstånd
        await write.SaveChangesAsync(ct);

        // 6) Svara med nya id utfallet
        return Utfall<VårdvalId>.Ok(vårdval.Id);
    }
}
