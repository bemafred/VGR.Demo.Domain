using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Linq;
using VGR.Technical.Testing;
using Xunit;
using Xunit.Abstractions;

namespace VGR.Semantics.Linq.Correlations;

/// <summary>
/// Korrelationstest för Tidsrymd – verifierar att domänmetoder och SQL är ekvivalenta.
/// Använder SqliteHarness för unified in-memory testning.
/// </summary>
public sealed class TidsrymdCorrelations
{
    private readonly ITestOutputHelper _output;

    public TidsrymdCorrelations(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Innehåller – Kritiska SQL-översättningsfall

    [Theory]
    [MemberData(nameof(TidsrymdTestScenarier.InnehållerScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task Innehåller_Korrelation_DomainEkvivalentSql(
        Tidsrymd period,
        DateTimeOffset tidpunkt,
        bool förväntat,
        string scenario)
    {
        // Arrange
        await using var h = new SqliteHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        person.SkapaVårdval(HsaId.Tolka("HSA-1"), period, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        // Act 1: Domänlogik (in-memory)
        var domainResult = period.Innehåller(tidpunkt);

        // Act 2: EF-översättning (SQL via WithSemantics)
        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.Period.Innehåller(tidpunkt))
            .AnyAsync();

        // Debug: Visa SQL för inspektionen
        var query = h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.Period.Innehåller(tidpunkt));
        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine("SQL:");
        _output.WriteLine(query.ToQueryString());
        _output.WriteLine("");

        // Assert: Domain och SQL måste ge samma resultat
        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion

    #region Överlappar – Overlap-semantik

    [Theory]
    [MemberData(nameof(TidsrymdTestScenarier.ÖverlapparScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task Överlappar_Korrelation_DomainEkvivalentSql(
        Tidsrymd p1,
        Tidsrymd p2,
        bool förväntat,
        string scenario)
    {
        // Arrange — två personer under samma region, olika enhet
        await using var h = new SqliteHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person1 = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        person1.SkapaVårdval(HsaId.Tolka("HSA-1"), p1, nu);
        var person2 = region.SkapaPerson(Personnummer.Tolka("19900202-5678"), nu);
        person2.SkapaVårdval(HsaId.Tolka("HSA-2"), p2, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        // Act 1: Domänlogik (in-memory)
        var domainResult = p1.Överlappar(p2);

        // Act 2: EF-översättning (SQL via WithSemantics)
        // Exkludera själv-matchning (v1 != v2) för korrekt överlappningskontroll
        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v1 => h.Read.Vårdval
                .Any(v2 => v2.Id != v1.Id && v1.Period.Överlappar(v2.Period)))
            .AnyAsync();

        // Debug: Visa SQL
        var query = h.Read.Vårdval
            .WithSemantics()
            .Where(v1 => h.Read.Vårdval
                .Any(v2 => v2.Id != v1.Id && v1.Period.Överlappar(v2.Period)));
        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine("SQL:");
        _output.WriteLine(query.ToQueryString());
        _output.WriteLine("");

        // Assert
        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion

    #region ÄrTillsvidare – NULL-hantering

    [Theory]
    [MemberData(nameof(TidsrymdTestScenarier.ÄrTillsvidareScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task ÄrTillsvidare_Korrelation_DomainEkvivalentSql(
        Tidsrymd period,
        bool förväntat,
        string scenario)
    {
        // Arrange
        await using var h = new SqliteHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        person.SkapaVårdval(HsaId.Tolka("HSA-1"), period, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        // Act 1: Domänlogik (in-memory)
        var domainResult = period.ÄrTillsvidare;

        // Act 2: EF-översättning (SQL via WithSemantics)
        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.Period.ÄrTillsvidare)
            .AnyAsync();

        // Debug
        var query = h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.Period.ÄrTillsvidare);
        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine("SQL:");
        _output.WriteLine(query.ToQueryString());
        _output.WriteLine("");

        // Assert
        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion

    #region ÄrAktivt (Vårdval) – Aggregat-expansion

    [Theory]
    [MemberData(nameof(TidsrymdTestScenarier.VårdvalÄrAktivtScenarier), MemberType = typeof(TidsrymdTestScenarier))]
    public async Task VårdvalÄrAktivt_Korrelation_DomainEkvivalentSql(
        Tidsrymd period,
        bool förväntat,
        string scenario)
    {
        // Arrange
        await using var h = new SqliteHarness();
        var nu = DateTimeOffset.UtcNow;

        var region = Region.Skapa("14");
        var person = region.SkapaPerson(Personnummer.Tolka("19900101-1234"), nu);
        var vårdval = person.SkapaVårdval(HsaId.Tolka("HSA-1"), period, nu);
        h.Write.Regioner.Add(region);
        await h.Write.SaveChangesAsync();

        // Act 1: Domänlogik (in-memory)
        var domainResult = vårdval.ÄrAktivt;

        // Act 2: EF-översättning (SQL via WithSemantics)
        // Vårdval.ÄrAktivt expansion delegerar till Tidsrymd.ÄrTillsvidare
        var sqlResult = await h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.ÄrAktivt)
            .AnyAsync();

        // Debug
        var query = h.Read.Vårdval
            .WithSemantics()
            .Where(v => v.ÄrAktivt);
        _output.WriteLine($"Scenario: {scenario}");
        _output.WriteLine("SQL (delegerad expansion):");
        _output.WriteLine(query.ToQueryString());
        _output.WriteLine("");

        // Assert
        Assert.Equal(förväntat, domainResult);
        Assert.Equal(domainResult, sqlResult);
    }

    #endregion
}
