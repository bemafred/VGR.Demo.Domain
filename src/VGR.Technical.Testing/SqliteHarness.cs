using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VGR.Infrastructure.EF;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace VGR.Technical.Testing;

public sealed class SqliteHarness : IAsyncDisposable
{
    private readonly SqliteConnection _conn;
    public ReadDbContext Read { get; }
    public WriteDbContext Write { get; }

    /// <summary>
    /// SQLite cannot translate DateTimeOffset comparisons (only equality and IS NULL).
    /// Store as binary (long) so integer comparison works.
    /// This is a provider concern — SQL Server handles DateTimeOffset natively.
    /// </summary>
    private static void SqliteDateTimeOffsetConventions(ModelConfigurationBuilder b)
    {
        b.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
        b.Properties<DateTimeOffset?>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    public SqliteHarness()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var readOpts = new DbContextOptionsBuilder<ReadDbContext>()
            .UseSqlite(_conn)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        var writeOpts = new DbContextOptionsBuilder<WriteDbContext>()
            .UseSqlite(_conn)
            .Options;

        Write = new WriteDbContext(writeOpts, SqliteDateTimeOffsetConventions);
        Read = new ReadDbContext(readOpts, SqliteDateTimeOffsetConventions);

        // Ensure schema
        Write.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await Write.DisposeAsync();
        await Read.DisposeAsync();
        await _conn.DisposeAsync();
    }
}
