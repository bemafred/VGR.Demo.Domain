using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Infrastructure.EF.Configs;

namespace VGR.Infrastructure.EF;

/// <summary>Skrivoptimerad DbContext med change tracking. Används för kommandon som ändrar tillstånd.</summary>
public sealed class WriteDbContext : DbContext
{
    private readonly Action<ModelConfigurationBuilder> _conventions;

    /// <summary>Alla personer.</summary>
    public DbSet<Person> Personer => Set<Person>();
    /// <summary>Alla vårdval.</summary>
    public DbSet<Vårdval> Vårdval => Set<Vårdval>();
    /// <summary>Alla regioner.</summary>
    public DbSet<Region> Regioner => Set<Region>();

    public WriteDbContext(DbContextOptions<WriteDbContext> options,
        Action<ModelConfigurationBuilder>? conventions = null) : base(options)
    {
        _conventions = conventions ?? (_ => { });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => _conventions(configurationBuilder);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfiguration(new PersonConfig());
        mb.ApplyConfiguration(new VårdvalConfig());
        mb.ApplyConfiguration(new RegionConfig());
    }
}