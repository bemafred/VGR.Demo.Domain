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

// DbContexts — PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("Vgr")
    ?? "Host=localhost;Database=vgr;Username=bemafred";

builder.Services.AddDbContext<ReadDbContext>(o =>
    o.UseNpgsql(connectionString)
     .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddDbContext<WriteDbContext>(o =>
    o.UseNpgsql(connectionString));

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

app.UseDomain(typeof(Region).Assembly);
app.MapControllers();
app.Run();
