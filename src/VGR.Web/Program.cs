using Microsoft.EntityFrameworkCore;
using VGR.Application.Personer;
using VGR.Application.Vårdval;
using VGR.Infrastructure.EF;
using VGR.Technical;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.Semantics.Queries;


var builder = WebApplication.CreateBuilder(args);

// DbContexts (example: in-memory for demo only)
builder.Services.AddDbContext<ReadDbContext>(o => o.UseInMemoryDatabase("vgr").UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddDbContext<WriteDbContext>(o => o.UseInMemoryDatabase("vgr"));

// Clock
builder.Services.AddSingleton<IClock, SystemClock>();

// Query semantics registry (manuell bootstrap ska fungera som alternativ till generatorn)
builder.Services.AddQuerySemantics(r =>
{
    r.Register<Tidsrymd, DateTimeOffset, bool>((range, t) => range.Innehåller(t),
                                               (range, t) => range.Start <= t && (range.Slut == null || t < range.Slut));
    r.Register<Vårdval, bool>(v => v.ÄrAktivt,
                              v => v.Period.Slut == null);
});

// Interactors
builder.Services.AddScoped<SkapaPersonInteractor>();
builder.Services.AddScoped<SkapaVårdvalInteractor>();

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
