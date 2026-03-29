using Microsoft.EntityFrameworkCore;
using VGR.Domain;
using VGR.Infrastructure.EF.Configs;

namespace VGR.Infrastructure.EF;

public sealed class WriteDbContext : DbContext
{
    private readonly Action<ModelConfigurationBuilder> _conventions;

    public DbSet<Person> Personer => Set<Person>();
    public DbSet<Vårdval> Vardval => Set<Vårdval>();
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