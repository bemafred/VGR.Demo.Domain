using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VGR.Domain.SharedKernel;
using VGR.Infrastructure.EF;
using VGR.Semantics.Linq;
using Xunit;
using Xunit.Abstractions;

namespace VGR.Semantics.Linq.CorrelationTests;

public sealed class TestTidsrymdEntity
{
    public int Id { get; set; }
    public Tidsrymd Period { get; set; }
}

/// <summary>
/// Korrelationstest för Tidsrymd - verifierar att Domain ≡ SQL.
/// Fokuserar på SQL-specifika översättningsscenarier.
/// </summary>
public sealed class TidsrymdCorrelationTests : IAsyncDisposable
{
    private readonly SqliteConnection _conn;
    private readonly CorrelationDbContext _db;
    private readonly ITestOutputHelper _output;

    public TidsrymdCorrelationTests(ITestOutputHelper output)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<CorrelationDbContext>()
            .UseSqlite(_conn)
            .Options;

        _db = new CorrelationDbContext(opts);
        _db.Database.EnsureCreated();
    }

    #region Innehåller - kritiska SQL-översättningsfall

    public static IEnumerable<object[]> InnehållerSqlScenarier()
    {
        var reftid = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

        yield return new object[] { Tidsrymd.SkapaTillsvidare(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)), reftid, true, "NULL-hantering (tillsvidare)" };
        yield return new object[] { Tidsrymd.Skapa(reftid, reftid.AddDays(30)), reftid, true, "Start inkluderad (<=)" };
        yield return new object[] { Tidsrymd.Skapa(reftid.AddDays(-30), reftid), reftid, false, "Slut exkluderad (<)" };
        yield return new object[] { Tidsrymd.Skapa(reftid.AddDays(-30), reftid.AddDays(30)), reftid, true, "AND-operator" };
    }

    [Theory]
    [MemberData(nameof(InnehållerSqlScenarier))]
    public async Task Innehåller_Korrelation_SqlOversättning(Tidsrymd period, DateTimeOffset tidpunkt, bool förväntat, string scenario)
    {
        // Arrange: spara testdata
        var entity = new TestTidsrymdEntity { Period = period };
        _db.TestEntities.Add(entity);
        await _db.SaveChangesAsync();

        // Act 1: Domänlogik (in-memory)
        var domainResult = period.Innehåller(tidpunkt);

        // Act 2: EF-översättning (SQL via WithSemantics)
        var sqlResult = await _db.TestEntities
            .WithSemantics()
            .Where(e => e.Period.Innehåller(tidpunkt))
            .AnyAsync();

        // Log SQL för debugging
        var query = _db.TestEntities
            .WithSemantics()
            .Where(e => e.Period.Innehåller(tidpunkt));
        _output.WriteLine($"SQL för '{scenario}':");
        _output.WriteLine(query.ToQueryString());

        // Assert: båda ger samma resultat
        Assert.Equal(domainResult, sqlResult);
        Assert.Equal(förväntat, domainResult);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _conn.DisposeAsync();
    }

    // Minimal DbContext för korrelationstester
    private sealed class CorrelationDbContext : DbContext
    {
        public DbSet<TestTidsrymdEntity> TestEntities => Set<TestTidsrymdEntity>();

        public CorrelationDbContext(DbContextOptions<CorrelationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<TestTidsrymdEntity>(b =>
            {
                b.HasKey(e => e.Id);
                b.ComplexProperty(e => e.Period, p =>
                {
                    p.Property(t => t.Start).HasColumnName("Start").IsRequired();
                    p.Property(t => t.Slut).HasColumnName("Slut");
                });
            });
        }
    }
}

