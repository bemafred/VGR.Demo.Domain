using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VGR.Infrastructure.EF;

namespace VGR.Technical.Testing;

/// <summary>
/// Testharness mot PostgreSQL. Speglar <see cref="SqliteHarness"/> men utan
/// DateTimeOffset-konventioner — PostgreSQL hanterar <c>timestamptz</c> nativt.
/// Skapar en unik databas per instans för full isolering vid parallella tester.
/// </summary>
public sealed class PostgresHarness : IAsyncDisposable
{
    private readonly string _databaseName;
    private readonly string _adminConnectionString;

    public ReadDbContext Read { get; }
    public WriteDbContext Write { get; }

    public PostgresHarness()
    {
        _databaseName = $"vgr_test_{Guid.NewGuid():N}";
        _adminConnectionString = "Host=localhost;Database=postgres;Username=bemafred";
        var cs = $"Host=localhost;Database={_databaseName};Username=bemafred";

        // Skapa isolerad testdatabas
        using (var adminConn = new NpgsqlConnection(_adminConnectionString))
        {
            adminConn.Open();
            using var cmd = adminConn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            cmd.ExecuteNonQuery();
        }

        var readOpts = new DbContextOptionsBuilder<ReadDbContext>()
            .UseNpgsql(cs)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        var writeOpts = new DbContextOptionsBuilder<WriteDbContext>()
            .UseNpgsql(cs)
            .Options;

        Write = new WriteDbContext(writeOpts);
        Read = new ReadDbContext(readOpts);

        Write.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await Write.DisposeAsync();
        await Read.DisposeAsync();

        // Radera testdatabasen
        using var adminConn = new NpgsqlConnection(_adminConnectionString);
        await adminConn.OpenAsync();

        // Avsluta alla anslutningar till testdatabasen innan drop
        using (var terminate = adminConn.CreateCommand())
        {
            terminate.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid()
                """;
            await terminate.ExecuteNonQueryAsync();
        }

        using var drop = adminConn.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
        await drop.ExecuteNonQueryAsync();
    }
}
