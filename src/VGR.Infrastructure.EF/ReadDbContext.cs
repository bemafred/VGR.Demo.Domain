using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Infrastructure.EF.Configs;

namespace VGR.Infrastructure.EF;

/// <summary>Läsoptimerad DbContext med <see cref="QueryTrackingBehavior.NoTracking"/>. Används för frågor utan sidoeffekter.</summary>
public sealed class ReadDbContext : DbContext
{
    private readonly Action<ModelConfigurationBuilder> _conventions;

    /// <summary>Alla personer.</summary>
    public DbSet<Person> Personer => Set<Person>();
    /// <summary>Alla vårdval.</summary>
    public DbSet<Vårdval> Vårdval => Set<Vårdval>();
    /// <summary>Alla regioner.</summary>
    public DbSet<Region> Regioner => Set<Region>();

    public ReadDbContext(DbContextOptions<ReadDbContext> options,
        Action<ModelConfigurationBuilder>? conventions = null) : base(options)
    {
        _conventions = conventions ?? (_ => { });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => _conventions(configurationBuilder);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        var slutFilter = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
            ? "\"Slut\" IS NULL"
            : "Slut IS NULL";

        mb.ApplyConfiguration(new PersonConfig());
        mb.ApplyConfiguration(new VårdvalConfig(slutFilter));
        mb.ApplyConfiguration(new RegionConfig());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder ob)
        => ob.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}