using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VGR.Infrastructure.EF;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace VGR.Technical.Testing;

public sealed class SqliteHarness : IAsyncDisposable
{
    private readonly SqliteConnection _conn;
    public ReadDbContext Read { get; }
    public WriteDbContext Write { get; }

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

        Write = new WriteDbContext(writeOpts);
        Read = new ReadDbContext(readOpts);

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
