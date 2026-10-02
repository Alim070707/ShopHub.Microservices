using Microsoft.AspNetCore.Mvc;
using CatalogService.DTOs;
using CatalogService.Services;

namespace CatalogService.Endpoints;

/// <summary>
/// Регистрация всех HTTP-эндпоинтов CatalogService.
/// Используем Minimal API: меньше бойлерплейта, лучше производительность.
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        // GET /api/products?category=Смартфоны&page=1&pageSize=20
        group.MapGet("/", async (
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            ProductService productService = default!,
            CancellationToken ct = default) =>
        {
            if (page < 1) page = 1;
            pageSize = Math.Min(pageSize, 100);

            var (items, total) = await productService.GetAllAsync(
                category, minPrice, maxPrice, page, pageSize, ct);

            return Results.Ok(new
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        })
        .WithSummary("Получить список товаров")
        .WithDescription("Фильтрация по категории и цене, пагинация.");

        // GET /api/products/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ProductService productService,
            CancellationToken ct) =>
        {
            var product = await productService.GetByIdAsync(id, ct);
            return product is null
                ? Results.NotFound(new { message = $"Товар {id} не найден" })
                : Results.Ok(product);
        })
        .WithSummary("Получить товар по ID");

        // GET /api/products/{id}/availability?quantity=2
        group.MapGet("/{id:guid}/availability", async (
            Guid id,
            [FromQuery] int quantity,
            ProductService productService,
            CancellationToken ct) =>
        {
            var result = await productService.CheckAvailabilityAsync(id, quantity, ct);
            return result is null
                ? Results.NotFound(new { message = $"Товар {id} не найден" })
                : Results.Ok(new
                {
                    ProductId = id,
                    ProductName = result.Value.Name,
                    RequestedQuantity = quantity,
                    InStock = result.Value.InStock,
                    CurrentPrice = result.Value.Price
                });
        })
        .WithSummary("Проверить наличие товара");

        // POST /api/products
        group.MapPost("/", async (
            CreateProductDto dto,
            ProductService productService,
            CancellationToken ct) =>
        {
            var created = await productService.CreateAsync(dto, ct);
            return Results.Created($"/api/products/{created.Id}", created);
        })
        .WithSummary("Создать новый товар");

        // PUT /api/products/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductDto dto,
            ProductService productService,
            CancellationToken ct) =>
        {
            var updated = await productService.UpdateAsync(id, dto, ct);
            return updated is null
                ? Results.NotFound(new { message = $"Товар {id} не найден" })
                : Results.Ok(updated);
        })
        .WithSummary("Обновить товар");

        // DELETE /api/products/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ProductService productService,
            CancellationToken ct) =>
        {
            var deleted = await productService.DeleteAsync(id, ct);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { message = $"Товар {id} не найден" });
        })
        .WithSummary("Удалить товар (soft delete)");
    }
}