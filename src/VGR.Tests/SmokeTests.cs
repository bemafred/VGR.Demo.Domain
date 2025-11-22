using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VGR.Application.Personer;
using VGR.Application.Vårdval;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Infrastructure.EF;
using VGR.Technical;
using VGR.Technical.Testing;
using Xunit;

using VGR.Domain.SharedKernel.Exceptions;
using VGR.Semantics.Linq;

namespace VGR.Tests;

public class SmokeTests
{
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => new DateTimeOffset(2024,1,1,12,0,0,TimeSpan.Zero); }

    [Fact]
    public async Task SkapaPerson_GerSkapaVårdvalLyckas()
    {
        await using var h = new SqliteHarness();

        var write = h.Write;
        var read = h.Read;
        var clock = new TestClock();
        var ct = CancellationToken.None;

        // Initiera en region
        var region = Region.Skapa("14");
        write.Regioner.Add(region);

        await write.SaveChangesAsync(ct);

        // Skapa person
        var skapaPerson = new SkapaPersonInteractor(read, write, clock);
        var cmdP = new SkapaPersonCmd(region.Id, "19900101-1234");
        var resP = await skapaPerson.ProcessAsync(cmdP, ct);
        Assert.True(resP.IsSuccess);

        var personId = resP.Value!; // Behåll som PersonId

        // Skapa vårdval
        var skapaVv = new SkapaVårdvalInteractor(write, clock);
        var cmdV = new SkapaVårdvalCmd(personId, "HSA-ENHET-1", new DateOnly(2024,1,1), null);
        var resV = await skapaVv.ProcessAsync(cmdV, ct);
        Assert.True(resV.IsSuccess);

        // Verifiera att det sparats
        var count = await read.Vårdval.CountAsync(v => v.PersonId == personId, ct);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SkapaVårdval_Överlapp_Kastar()
    {
        await using var h = new SqliteHarness();
        var write = h.Write;
        var read = h.Read;
        var clock = new TestClock();
        var ct = CancellationToken.None;

        // Initiera region och person via aggregat för att undvika dubbla spårade instanser
        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Parse("19900101-1234"), clock.UtcNow);

        write.Regioner.Add(region);
        await write.SaveChangesAsync(ct);

        var interactor = new SkapaVårdvalInteractor(write, clock);

        var ok = await interactor.ProcessAsync(
            new SkapaVårdvalCmd(person.Id, "HSA-ENHET-1", new DateOnly(2024,1,1), new DateOnly(2024,12,31)),
            ct);
        Assert.True(ok.IsSuccess);

        // Förvänta domän-exception, inte Utfall.Fail
        await Assert.ThrowsAsync<DomainInvariantViolationException>(async () =>
            await interactor.ProcessAsync(
                new SkapaVårdvalCmd(person.Id, "HSA-ENHET-1", new DateOnly(2024, 6, 1), null),
                ct));
    }
}
