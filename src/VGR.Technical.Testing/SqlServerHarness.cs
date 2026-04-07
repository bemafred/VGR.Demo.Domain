using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VGR.Infrastructure.EF;

namespace VGR.Technical.Testing;

/// <summary>
/// Testharness mot SQL Server. Speglar <see cref="PostgresHarness"/> men mot lokal
/// SQL Server default-instans med integrerad autentisering.
/// Skapar en unik databas per instans för full isolering vid parallella tester.
/// </summary>
public sealed class SqlServerHarness : IAsyncDisposable
{
    private readonly string _databaseName;
    private readonly string _adminConnectionString;

    public ReadDbContext Read { get; }
    public WriteDbContext Write { get; }

    public SqlServerHarness()
    {
        _databaseName = $"vgr_test_{Guid.NewGuid():N}";
        _adminConnectionString = "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
        var cs = $"Server=.;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        // Skapa isolerad testdatabas
        using (var adminConn = new SqlConnection(_adminConnectionString))
        {
            adminConn.Open();
            using var cmd = adminConn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{_databaseName}]";
            cmd.ExecuteNonQuery();
        }

        var readOpts = new DbContextOptionsBuilder<ReadDbContext>()
            .UseSqlServer(cs)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        var writeOpts = new DbContextOptionsBuilder<WriteDbContext>()
            .UseSqlServer(cs)
            .Options;

        Write = new WriteDbContext(writeOpts);
        Read = new ReadDbContext(readOpts);

        Write.Database.EnsureCreated();
    }

    public static bool ÄrTillgänglig()
    {
        try
        {
            using var conn = new SqlConnection("Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
            conn.Open();
            return true;
        }
        catch { return false; }
    }

    public async ValueTask DisposeAsync()
    {
        await Write.DisposeAsync();
        await Read.DisposeAsync();

        // Radera testdatabasen
        using var adminConn = new SqlConnection(_adminConnectionString);
        await adminConn.OpenAsync();

        // Stäng alla anslutningar till testdatabasen innan drop
        using (var alter = adminConn.CreateCommand())
        {
            alter.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
            await alter.ExecuteNonQueryAsync();
        }

        using var drop = adminConn.CreateCommand();
        drop.CommandText = $"DROP DATABASE [{_databaseName}]";
        await drop.ExecuteNonQueryAsync();
    }
}
