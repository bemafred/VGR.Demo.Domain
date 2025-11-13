
using Microsoft.EntityFrameworkCore;
using VGR.Application.Personer;
using VGR.Application.Vårdval;
using VGR.Infrastructure.EF;
using VGR.Technical;
using VGR.Domain;
using VGR.Domain.SharedKernel;
using VGR.QuerySemantics;


var builder = WebApplication.CreateBuilder(args);

// DbContexts (example: in-memory for demo only)
builder.Services.AddDbContext<ReadDbContext>(o => o.UseInMemoryDatabase("vgr").UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddDbContext<WriteDbContext>(o => o.UseInMemoryDatabase("vgr"));

// Clock
builder.Services.AddSingleton<IClock, SystemClock>();

// Query semantics registry (manuell bootstrap tills generatorn exponerar en registry)
var semantics = new QuerySemantics()
    .Register<Tidsrymd, DateTimeOffset, bool>((r, t) => r.Innehåller(t),
        (r, t) => r.Start <= t && (r.Slut == null || t < r.Slut))
    .Register<Vårdval, DateTimeOffset, bool>((v, t) => v.ÄrAktivt(t),
        (v, t) => v.Period.Start <= t && (v.Period.Slut == null || t < v.Period.Slut));
builder.Services.AddSingleton(semantics);

// Interactors
builder.Services.AddScoped<SkapaPersonInteractor>();
builder.Services.AddScoped<SkapaVårdvalInteractor>();

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
