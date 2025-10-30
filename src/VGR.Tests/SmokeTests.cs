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
using Xunit;

namespace VGR.Tests;

public class SmokeTests
{
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => new DateTimeOffset(2024,1,1,12,0,0,TimeSpan.Zero); }

    [Fact]
    public async Task CreatePerson_Then_CreateVårdval_Succeeds()
    {
        await using var h = new SqliteHarness();
        var write = h.Write;
        var read = h.Read;
        var clock = new TestClock();
        var ct = CancellationToken.None;

        // Seed a Region
        var region = Region.Skapa("14");
        write.Regioner.Add(region);

        await write.SaveChangesAsync(ct);

        // Create person
        var skapaPerson = new SkapaPersonInteractor(read, write, clock);
        var cmdP = new SkapaPersonCmd(region.Id, "19900101-1234");
        var resP = await skapaPerson.ProcessAsync(cmdP, ct);
        Assert.True(resP.IsSuccess);

        var personId = resP.Value!; // keep as PersonId

        // Create vardval
        var skapaVv = new SkapaVårdvalInteractor(read, write, clock);
        var cmdV = new SkapaVårdvalCmd(personId, "HSA-ENHET-1", "HSA-LAKARE-1", new DateOnly(2024,1,1), null);
        var resV = await skapaVv.ProcessAsync(cmdV, ct);
        Assert.True(resV.IsSuccess);

        // Verify persisted
        var count = await read.Vardval.CountAsync(v => v.PersonId == personId, ct);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateVardval_Overlapping_ReturnsFail()
    {
        await using var h = new SqliteHarness();
        var write = h.Write;
        var read = h.Read;
        var clock = new TestClock();
        var ct = CancellationToken.None;

        // Seed region + person via aggregate to avoid duplicate tracked instances
        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Parse("19900101-1234"), clock.UtcNow);

        write.Regioner.Add(region);
        await write.SaveChangesAsync(ct);

        var interactor = new SkapaVårdvalInteractor(read, write, clock);

        var ok = await interactor.ProcessAsync(
            new SkapaVårdvalCmd(person.Id, "HSA-ENHET-1", "HSA-LAKARE-1", new DateOnly(2024,1,1), new DateOnly(2024,12,31)),
            ct);
        Assert.True(ok.IsSuccess);

        var fail = await interactor.ProcessAsync(
            new SkapaVårdvalCmd(person.Id, "HSA-ENHET-1", "HSA-LAKARE-1", new DateOnly(2024,6,1), null),
            ct);
        Assert.False(fail.IsSuccess);
    }
}
