using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Technical.Testing;

namespace VGR.Infrastructure.PostgreSQL.Verifications;

/// <summary>
/// Verifierar EF-konfiguration och persistens mot PostgreSQL.
/// Speglar <c>VGR.Infrastructure.Diagnostics</c> men mot riktig databas.
/// </summary>
public sealed class PostgresInfrastrukturVerifieringar
{
    [KräverPostgresFact]
    public async Task Schema_SkaparTabeller()
    {
        await using var h = new PostgresHarness();

        var tables = await h.Read.Database
            .SqlQueryRaw<string>(
                "SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename")
            .ToListAsync();

        tables.Should().Contain("Person")
              .And.Contain("Region")
              .And.Contain("Vårdval");
    }

    [KräverPostgresFact]
    public async Task Schema_FiltreratUniktIndex_EttAktivtVårdvalPerPerson()
    {
        await using var h = new PostgresHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);

        // Första tillsvidare-vårdvalet — ska lyckas
        person.SkapaVårdval(HsaId.Tolka("HSA-ENHET-1"),
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)), nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        // Andra tillsvidare-vårdvalet för samma person — ska krascha på unikt index
        var person2 = await h.Write.Personer
            .Include(p => p.AllaVårdval)
            .FirstAsync(p => p.Id == person.Id);

        // Kringgå domäninvarianten genom att skapa via en annan person-instans med annan enhet
        // men det filtrerade indexet (PersonId WHERE Slut IS NULL) ska förhindra det på databasnivå.
        // Vi lägger in direkt via SQL för att testa indexet isolerat.
        var act = async () => await h.Write.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Vårdval\" (\"Id\", \"PersonId\", \"EnhetsHsaId\", \"SkapadTid\", \"RowVersion\", \"Start\") " +
            "VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            Guid.NewGuid(), person.Id.Value, "HSA-ENHET-2", nu, new byte[] { 0 }, nu);

        await act.Should().ThrowAsync<Exception>()
            .Where(ex => ex.Message.Contains("IX_Vårdval_PersonId"));
    }

    [KräverPostgresFact]
    public async Task ReadDbContext_LeverarDetachedEntiteter()
    {
        await using var h = new PostgresHarness();

        var region = Region.Skapa("14");
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var loaded = await h.Read.Regioner.FirstAsync();

        h.Read.Entry(loaded).State.Should().Be(EntityState.Detached);
    }

    [KräverPostgresFact]
    public async Task ValueObjectKonvertering_Personnummer_Rundresa()
    {
        await using var h = new PostgresHarness();

        var region = Region.Skapa("14");
        var pnr = Personnummer.Tolka("19900101-1234");
        region.SkapaPerson(pnr, DateTimeOffset.UtcNow);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var person = await h.Read.Personer.FirstAsync();

        person.Personnummer.Should().Be(pnr);
    }

    [KräverPostgresFact]
    public async Task ComplexProperty_Period_SparasOchLäses()
    {
        await using var h = new PostgresHarness();
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

    [KräverPostgresFact]
    public async Task RowVersion_SättsVidSparning()
    {
        await using var h = new PostgresHarness();

        var region = Region.Skapa("14");
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var loaded = await h.Write.Regioner.FirstAsync();

        loaded.RowVersion.Should().NotBeNullOrEmpty();
    }
}
