using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Infrastructure.EF;
using VGR.Technical;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Semantics.Linq;

namespace VGR.Application.Vårdval;

/// <summary>Kommando för att skapa ett vårdval för en person vid en vårdenhet.</summary>
public sealed record SkapaVårdvalCmd(PersonId PersonId, string EnhetsHsaId, DateOnly Start, DateOnly? Slut);

/// <summary>Interaktor: skapar ett nytt vårdval. Avslutar eventuellt aktivt vårdval. Kastar vid överlapp eller saknad person.</summary>
public sealed class SkapaVårdvalInteractor(WriteDbContext write, IClock clock)
{
    /// <summary>Utför kommandot. Kastar vid saknad person, ogiltigt HSA-ID eller överlappande perioder.</summary>
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
            .WithSemantics()
            .Where(vårdval => vårdval.ÄrAktivt)
            .LoadAsync(ct);   

        // 4) Skapa vårdvalet via domänen (stänger gällande, öppet vårdval)
        var vårdval = person.SkapaVårdval(enhet, giltighet, clock.UtcNow);

        // 5) Uppdatera datalagrets tillstånd
        await write.SaveChangesAsync(ct);

        // 6) Svara med nya id utfallet
        return Utfall<VårdvalId>.Ok(vårdval.Id);
    }
}
