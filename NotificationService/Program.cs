using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Consumers;
using NotificationService.Data;
using NotificationService.Services;
using Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

// ── БД ────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Email ────────────────────────────────────────────────────────────────
// ВАЖНО: для КТ-4 всегда используем FakeEmailService —
// реальный SMTP настроить в рамках задания не требуется.
// В продакшене здесь был бы выбор через builder.Environment.IsDevelopment().
builder.Services.AddScoped<IEmailService, FakeEmailService>();

// ── MassTransit: подписываемся на событие OrderCreated ───────────────────
builder.Services.AddMassTransit(x =>
{
    // Регистрируем consumer
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        // Привязка к exchange shop.order.created
        cfg.Message<OrderCreatedEvent>(m => m.SetEntityName("shop.order.created"));

        // MassTransit сам создаст очередь для consumer'а
        cfg.ConfigureEndpoints(context);
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "postgresql",
        tags: new[] { "db", "ready" });

var app = builder.Build();

// ── Автоматические миграции при старте ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

// NotificationService не имеет публичного API — только health и корень
app.MapHealthChecks("/health");
app.MapGet("/", () => "NotificationService is running");

app.Run();