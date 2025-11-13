
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

// Query semantics registry (manuell bootstrap tills generatorn exponerar en registry)
var semantic = new SemanticMappings()
    .Register<Tidsrymd, DateTimeOffset, bool>((r, t) => r.Innehåller(t),
        (r, t) => r.Start <= t && (r.Slut == null || t < r.Slut))
    .Register<Vårdval, bool>((v) => v.ÄrAktivt,
        (v) => v.Period.Slut == null);

builder.Services.AddSingleton(semantic);

// Interactors
builder.Services.AddScoped<SkapaPersonInteractor>();
builder.Services.AddScoped<SkapaVårdvalInteractor>();

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
