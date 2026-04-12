using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Linq;
using VGR.Technical.Testing;
using Xunit.Abstractions;

namespace VGR.Infrastructure.SqlServer.Verifications;

/// <summary>
/// Korrelationstest mot SQL Server — verifierar att domänmetoder och genererad SQL
/// är ekvivalenta med SQL Servers <c>datetimeoffset</c>-semantik.
/// </summary>
public sealed class SqlServerKorrelationer
{
    private readonly ITestOutputHelper _output;

    public SqlServerKorrelationer(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Innehåller

    [KräverSqlServerTheory]
    [MemberData(nameof(TidsrymdTestScenarier.InnehållerScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task Innehåller_DomänEkvivalentSqlServer(
        Tidsrymd period, DateTimeOffset tidpunkt, bool förväntat, string scenario)
    {
        await using var h = new SqlServerHarness();
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

    [KräverSqlServerTheory]
    [MemberData(nameof(TidsrymdTestScenarier.ÖverlapparScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task Överlappar_DomänEkvivalentSqlServer(
        Tidsrymd p1, Tidsrymd p2, bool förväntat, string scenario)
    {
        await using var h = new SqlServerHarness();
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

    [KräverSqlServerTheory]
    [MemberData(nameof(TidsrymdTestScenarier.ÄrTillsvidareScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task ÄrTillsvidare_DomänEkvivalentSqlServer(
        Tidsrymd period, bool förväntat, string scenario)
    {
        await using var h = new SqlServerHarness();
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

    [KräverSqlServerTheory]
    [MemberData(nameof(TidsrymdTestScenarier.VårdvalÄrAktivtScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task VårdvalÄrAktivt_DomänEkvivalentSqlServer(
        Tidsrymd period, bool förväntat, string scenario)
    {
        await using var h = new SqlServerHarness();
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
