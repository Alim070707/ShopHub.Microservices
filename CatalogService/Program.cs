using Microsoft.EntityFrameworkCore;
using CatalogService.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Swagger/OpenAPI ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── База данных (PostgreSQL через EF Core) ────────────────────────────────
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// ── Swagger UI (только в dev) ─────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Автоматические миграции при старте ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/", () => "CatalogService is running");

app.Run();