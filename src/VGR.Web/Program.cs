
using Microsoft.EntityFrameworkCore;
using VGR.Application.Personer;
using VGR.Application.Vårdval;
using VGR.Infrastructure.EF;
using VGR.Technical;

var builder = WebApplication.CreateBuilder(args);

// DbContexts (example: in-memory for demo only)
builder.Services.AddDbContext<ReadDbContext>(o => o.UseInMemoryDatabase("vgr").UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddDbContext<WriteDbContext>(o => o.UseInMemoryDatabase("vgr"));

// Clock
builder.Services.AddSingleton<IClock, SystemClock>();

// Interactors
builder.Services.AddScoped<SkapaPersonInteractor>();
builder.Services.AddScoped<SkapaVårdvalInteractor>();

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
