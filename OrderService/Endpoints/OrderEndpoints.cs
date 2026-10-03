using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Endpoints;

/// <summary>
/// HTTP-эндпоинты OrderService.
/// Заголовки X-User-Id / X-User-Email устанавливает API Gateway (в КТ-4 — эмулируем).
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // POST /api/orders
        group.MapPost("/", async (
            CreateOrderDto dto,
            HttpContext ctx,
            OrderManagementService service,
            CancellationToken ct) =>
        {
            var userIdStr = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
            var userEmail = ctx.Request.Headers["X-User-Email"].FirstOrDefault();

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrEmpty(userEmail))
                return Results.BadRequest(new { message = "X-User-Email обязателен" });

            try
            {
                var order = await service.CreateAsync(dto, userId, userEmail, ct);
                return Results.Created($"/api/orders/{order.Id}", order);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return Results.StatusCode(502);  // Bad Gateway — CatalogService недоступен
            }
        })
        .WithSummary("Создать заказ");

        // GET /api/orders
        group.MapGet("/", async (
            HttpContext ctx,
            OrderManagementService service,
            CancellationToken ct) =>
        {
            var userIdStr = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var orders = await service.GetUserOrdersAsync(userId, ct);
            return Results.Ok(orders);
        })
        .WithSummary("Список заказов пользователя");

        // GET /api/orders/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            OrderManagementService service,
            CancellationToken ct) =>
        {
            var order = await service.GetByIdAsync(id, ct);
            return order is null
                ? Results.NotFound(new { message = $"Заказ {id} не найден" })
                : Results.Ok(order);
        })
        .WithSummary("Детали заказа");
    }
}