using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Technical.Testing;

namespace VGR.Infrastructure.Diagnostics;

public class DbContextDiagnostics
{
    [Fact]
    public async Task ReadDbContext_LeverarDetachedEntiteter()
    {
        await using var h = new SqliteHarness();

        var region = Region.Skapa("14");
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var loaded = await h.Read.Regioner.FirstAsync();

        h.Read.Entry(loaded).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task WriteDbContext_SpårarEntiteter()
    {
        await using var h = new SqliteHarness();

        var region = Region.Skapa("14");
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var loaded = await h.Write.Regioner.FirstAsync();

        h.Write.Entry(loaded).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task ValueObjectKonvertering_Personnummer_Rundresa()
    {
        await using var h = new SqliteHarness();

        var region = Region.Skapa("14");
        var pnr = Personnummer.Tolka("19900101-1234");
        region.SkapaPerson(pnr, DateTimeOffset.UtcNow);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var person = await h.Read.Personer.FirstAsync();

        person.Personnummer.Should().Be(pnr);
    }

    [Fact]
    public async Task ComplexProperty_Period_SparasOchLäses()
    {
        await using var h = new SqliteHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        var period = Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        person.SkapaVårdval(HsaId.Tolka("HSA-ENHET-1"), period, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var vårdval = await h.Read.Vårdval.FirstAsync();

        vårdval.Period.Start.Should().Be(period.Start);
        vårdval.Period.Slut.Should().BeNull();
        vårdval.Period.ÄrTillsvidare.Should().BeTrue();
    }

    [Fact]
    public async Task Schema_SkaparTabeller()
    {
        await using var h = new SqliteHarness();

        var tables = await h.Read.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
            .ToListAsync();

        tables.Should().Contain("Region")
              .And.Contain("Person")
              .And.Contain("Vårdval");
    }

    [Fact]
    public async Task RowVersion_SättsVidSparning()
    {
        await using var h = new SqliteHarness();

        var region = Region.Skapa("14");
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var loaded = await h.Write.Regioner.FirstAsync();

        loaded.RowVersion.Should().NotBeNullOrEmpty();
    }
}
