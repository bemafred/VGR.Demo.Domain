using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Infrastructure.EF;
using VGR.Technical;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel.Exceptions;
using VGR.Semantics.Linq;

namespace VGR.Application.Personer;

/// <summary>Kommando för att skapa en person inom en region.</summary>
public sealed record SkapaPersonCmd(RegionId RegionId, string Personnummer);

/// <summary>Interaktor: skapar en ny person i en region. Kontrollerar dubbletter och validerar personnummer.</summary>
public sealed class SkapaPersonInteractor(ReadDbContext read, WriteDbContext write, IClock clock)
{
    /// <summary>Utför kommandot. Returnerar <c>Utfall.Fail</c> vid dubblett, kastar vid saknad region.</summary>
    public async Task<Utfall<PersonId>> ProcessAsync(SkapaPersonCmd cmd, CancellationToken ct)
    {
        var pnr = Personnummer.Tolka(cmd.Personnummer);

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
