using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using VGR.Application.Personer;
using VGR.Application.Vårdval;
using VGR.Infrastructure.EF;
using VGR.Technical;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Linq;
using VGR.Technical.Web;


var builder = WebApplication.CreateBuilder(args);

// DbContexts — PostgreSQL (Mac) / SQL Server (Windows)
var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

var connectionString = builder.Configuration.GetConnectionString("Vgr")
    ?? (isWindows
        ? "Server=.;Database=vgr;Trusted_Connection=True;TrustServerCertificate=True"
        : "Host=localhost;Database=vgr;Username=bemafred");

void ConfigureProvider(DbContextOptionsBuilder o) =>
    _ = isWindows ? o.UseSqlServer(connectionString) : o.UseNpgsql(connectionString);

// CQRS-light - läs kontexten
builder.Services.AddDbContext<ReadDbContext>(o =>
{
    ConfigureProvider(o);
    o.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// CQRS-light - skriv kontexten
builder.Services.AddDbContext<WriteDbContext>(ConfigureProvider);

// Clock
builder.Services.AddSingleton<IClock, SystemClock>();

// Interactors
builder.Services.AddScoped<SkapaPersonInteractor>();
builder.Services.AddScoped<SkapaVårdvalInteractor>();

builder.Services.AddControllers();
var app = builder.Build();

// Säkerställ att schemat finns (code-first, inga migrations)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
    db.Database.EnsureCreated();
}

SemanticRegistry.UseDomain(typeof(Region).Assembly);
app.MapDomainEndpoints();
app.MapControllers();

app.Run();
