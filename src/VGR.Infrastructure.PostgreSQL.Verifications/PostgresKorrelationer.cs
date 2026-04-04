using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Linq;
using VGR.Technical.Testing;
using Xunit.Abstractions;

namespace VGR.Infrastructure.PostgreSQL.Verifications;

/// <summary>
/// Korrelationstest mot PostgreSQL — verifierar att domänmetoder och genererad SQL
/// är ekvivalenta med <c>timestamptz</c>-semantik (inte SQLite binary-konvertering).
/// </summary>
public sealed class PostgresKorrelationer
{
    private readonly ITestOutputHelper _output;

    public PostgresKorrelationer(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Innehåller

    public static IEnumerable<object[]> InnehållerScenarier()
    {
        var reftid = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

        yield return new object[]
        {
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            reftid, true, "NULL-hantering (tillsvidare)"
        };
        yield return new object[]
        {
            Tidsrymd.Skapa(reftid, reftid.AddDays(30)),
            reftid, true, "Start inkluderad (<=)"
        };
        yield return new object[]
        {
            Tidsrymd.Skapa(reftid.AddDays(-30), reftid),
            reftid, false, "Slut exkluderad (<)"
        };
        yield return new object[]
        {
            Tidsrymd.Skapa(reftid.AddDays(-30), reftid.AddDays(30)),
            reftid, true, "AND-operator"
        };
    }

    [Theory]
    [MemberData(nameof(InnehållerScenarier))]
    public async Task Innehåller_DomänEkvivalentPostgreSQL(
        Tidsrymd period, DateTimeOffset tidpunkt, bool förväntat, string scenario)
    {
        await using var h = new PostgresHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        person.SkapaVårdval(HsaId.Tolka("HSA-1"), period, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var domainResult = period.Innehåller(tidpunkt);

        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.Period.Innehåller(tidpunkt))
            .AnyAsync();

        var query = h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.Period.Innehåller(tidpunkt));
        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine($"SQL: {query.ToQueryString()}");

        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion

    #region Överlappar

    public static IEnumerable<object[]> ÖverlapparScenarier()
    {
        var p1Start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var p1End = new DateTimeOffset(2024, 6, 30, 0, 0, 0, TimeSpan.Zero);
        var periode1 = Tidsrymd.Skapa(p1Start, p1End);

        yield return new object[]
        {
            periode1,
            Tidsrymd.Skapa(new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Helt skilda intervall"
        };
        yield return new object[]
        {
            periode1,
            Tidsrymd.Skapa(new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 4, 30, 0, 0, 0, TimeSpan.Zero)),
            true, "Helt överlappande"
        };
        yield return new object[]
        {
            periode1,
            Tidsrymd.Skapa(new DateTimeOffset(2024, 6, 30, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Angränsande – inte överlappande"
        };
        yield return new object[]
        {
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            periode1,
            true, "Tillsvidare möter begränsad"
        };
        yield return new object[]
        {
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero)),
            true, "Båda tillsvidare"
        };
    }

    [Theory]
    [MemberData(nameof(ÖverlapparScenarier))]
    public async Task Överlappar_DomänEkvivalentPostgreSQL(
        Tidsrymd p1, Tidsrymd p2, bool förväntat, string scenario)
    {
        await using var h = new PostgresHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person1 = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        person1.SkapaVårdval(HsaId.Tolka("HSA-1"), p1, nu);
        var person2 = region.SkapaPerson(Personnummer.Tolka("19900202-5678"), nu);
        person2.SkapaVårdval(HsaId.Tolka("HSA-2"), p2, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var domainResult = p1.Överlappar(p2);

        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v1 => h.Read.Vårdval
                .Any(v2 => v2.Id != v1.Id && v1.Period.Överlappar(v2.Period)))
            .AnyAsync();

        var query = h.Read.Vårdval
            .WithSemantics()
            .Where(v1 => h.Read.Vårdval
                .Any(v2 => v2.Id != v1.Id && v1.Period.Överlappar(v2.Period)));
        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine($"SQL: {query.ToQueryString()}");

        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion

    #region ÄrTillsvidare

    public static IEnumerable<object[]> ÄrTillsvidareScenarier()
    {
        yield return new object[]
        {
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            true, "Slut är null"
        };
        yield return new object[]
        {
            Tidsrymd.Skapa(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Slut är satt"
        };
    }

    [Theory]
    [MemberData(nameof(ÄrTillsvidareScenarier))]
    public async Task ÄrTillsvidare_DomänEkvivalentPostgreSQL(
        Tidsrymd period, bool förväntat, string scenario)
    {
        await using var h = new PostgresHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        person.SkapaVårdval(HsaId.Tolka("HSA-1"), period, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var domainResult = period.ÄrTillsvidare;

        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.Period.ÄrTillsvidare)
            .AnyAsync();

        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine($"SQL: {h.Read.Vårdval.WithSemantics().Where(v => v.Period.ÄrTillsvidare).ToQueryString()}");

        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion

    #region ÄrAktivt (Vårdval)

    public static IEnumerable<object[]> VårdvalÄrAktivtScenarier()
    {
        yield return new object[]
        {
            Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            true, "Vårdval är aktivt (tillsvidare)"
        };
        yield return new object[]
        {
            Tidsrymd.Skapa(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                           new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            false, "Vårdval är avslutat"
        };
    }

    [Theory]
    [MemberData(nameof(VårdvalÄrAktivtScenarier))]
    public async Task VårdvalÄrAktivt_DomänEkvivalentPostgreSQL(
        Tidsrymd period, bool förväntat, string scenario)
    {
        await using var h = new PostgresHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        var vårdval = person.SkapaVårdval(HsaId.Tolka("HSA-1"), period, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        var domainResult = vårdval.ÄrAktivt;

        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.ÄrAktivt)
            .AnyAsync();

        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine($"SQL: {h.Read.Vårdval.WithSemantics().Where(v => v.ÄrAktivt).ToQueryString()}");

        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion
}
