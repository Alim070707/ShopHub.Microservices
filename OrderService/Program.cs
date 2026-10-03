using Microsoft.EntityFrameworkCore;
using Polly;
using OrderService.Data;
using OrderService.Endpoints;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Swagger ───────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "OrderService API", Version = "v1" });
});

// ── База данных ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── HTTP-клиент для CatalogService с Polly ────────────────────────────────
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatalogService:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddTransientHttpErrorPolicy(policy =>
    policy.WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, attempt, _) =>
        {
            Console.WriteLine(
                $"⚠️ CatalogService retry #{attempt} через {timespan.TotalMilliseconds}ms: {outcome.Exception?.Message}");
        }))
.AddTransientHttpErrorPolicy(policy =>
    policy.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (_, duration) =>
            Console.WriteLine($"⚡ Circuit Breaker ОТКРЫТ на {duration.TotalSeconds}s"),
        onReset: () =>
            Console.WriteLine("✅ Circuit Breaker ЗАКРЫТ — CatalogService снова доступен")));

// ── Сервисы ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<OrderManagementService>();

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "postgresql",
        tags: new[] { "db", "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.MigrateAsync();
}

app.MapOrderEndpoints();
app.MapHealthChecks("/health");
app.MapGet("/", () => "OrderService is running");

app.Run();