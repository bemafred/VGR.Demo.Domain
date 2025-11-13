using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Infrastructure.EF;
using VGR.Technical;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Semantics.Queries;

namespace VGR.Application.Personer;

public sealed record SkapaPersonCmd(RegionId RegionId, string Personnummer);

public sealed class SkapaPersonInteractor(ReadDbContext read, WriteDbContext write, IClock clock, Semantic semantic)
{
    public async Task<Utfall<PersonId>> ProcessAsync(SkapaPersonCmd cmd, CancellationToken ct)
    {
        var pnr = Personnummer.Parse(cmd.Personnummer);

        // Pushdown: dubblett inom region?
        var dubblett = await read.Personer
            .AnyAsync(p => p.RegionId == cmd.RegionId && p.Personnummer == pnr, ct); 

        if (dubblett)
            return Utfall<PersonId>.Fail($"Personnummer redan registrerat: {pnr}"); // <-- Billigt svar

        // Ladda region (tracking) för att nyttja fabriken för Person
        var region = await write.Regioner.FirstOrDefaultAsync(r => r.Id == cmd.RegionId, ct);

        if (region is null)
            Throw.Region.Saknas(cmd.RegionId);

        var person = region.SkapaPerson(pnr, clock.UtcNow);

        await write.SaveChangesAsync(ct);

        return Utfall<PersonId>.Ok(person.Id);
    }
}
