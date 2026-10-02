using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Endpoints;
using CatalogService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Swagger/OpenAPI ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "CatalogService API",
        Version = "v1",
        Description = "Управление каталогом товаров ShopHub"
    });
});

// ── База данных (PostgreSQL) ─────────────────────────────────────────────
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Redis-кэш ─────────────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "catalog:";
});

// ── Сервисы (DI) ──────────────────────────────────────────────────────────
builder.Services.AddScoped<ProductService>();

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("Default")!,
        name: "postgresql",
        tags: new[] { "db", "ready" })
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "cache", "ready" });

var app = builder.Build();

// ── Swagger UI (только в dev) ─────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogService v1"));
}

// ── Автоматические миграции при старте ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
}

// ── Эндпоинты ─────────────────────────────────────────────────────────────
app.MapProductEndpoints();
app.MapHealthChecks("/health");

app.MapGet("/", () => "CatalogService is running");

app.Run();