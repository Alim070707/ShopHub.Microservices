using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CatalogService.Data;
using CatalogService.DTOs;
using CatalogService.Models;

namespace CatalogService.Services;

/// <summary>
/// Сервис управления товарами каталога.
/// Содержит всю бизнес-логику: CRUD + кэширование в Redis.
/// </summary>
public class ProductService
{
    private readonly CatalogDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProductService> _logger;

    // Время жизни кэша — 5 минут
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ProductService(
        CatalogDbContext db,
        IDistributedCache cache,
        ILogger<ProductService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Получить список активных товаров с фильтрацией и пагинацией.
    /// Список не кэшируется — часто меняется.
    /// </summary>
    public async Task<(List<ProductDto> Items, int TotalCount)> GetAllAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Products.Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <summary>
    /// Получить товар по ID. Использует Redis-кэш (5 минут).
    /// </summary>
    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = $"product:{id}";

        // Сначала смотрим в Redis
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Товар {ProductId} получен из кэша", id);
                return JsonSerializer.Deserialize<ProductDto>(cached);
            }
        }
        catch (Exception ex)
        {
            // Redis упал — не страшно, идём в БД
            _logger.LogWarning(ex, "Ошибка чтения из Redis, идём в БД");
        }

        // Redis промахнулся — идём в PostgreSQL
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);

        if (product is null) return null;

        var dto = ToDto(product);

        // Кладём в кэш
        try
        {
            var json = JsonSerializer.Serialize(dto);
            await _cache.SetStringAsync(cacheKey, json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось сохранить товар в кэш");
        }

        return dto;
    }

    /// <summary>Создать новый товар.</summary>
    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            Category = dto.Category,
            ImageUrl = dto.ImageUrl
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Создан товар {ProductId}: {ProductName}", product.Id, product.Name);

        return ToDto(product);
    }

    /// <summary>Обновить товар. Возвращает null, если не найден.</summary>
    public async Task<ProductDto?> UpdateAsync(
        Guid id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is null || !product.IsActive) return null;

        // Обновляем только переданные поля
        if (dto.Name is not null) product.Name = dto.Name;
        if (dto.Description is not null) product.Description = dto.Description;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;
        if (dto.Category is not null) product.Category = dto.Category;
        if (dto.ImageUrl is not null) product.ImageUrl = dto.ImageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Инвалидируем кэш
        await _cache.RemoveAsync($"product:{id}", ct);

        return ToDto(product);
    }

    /// <summary>Мягкое удаление (soft delete): IsActive = false.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product is null) return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync($"product:{id}", ct);

        _logger.LogInformation("Товар {ProductId} помечен как удалённый", id);
        return true;
    }

    /// <summary>Проверить наличие товара и узнать текущую цену (для OrderService).</summary>
    public async Task<(bool InStock, decimal Price, string Name)?> CheckAvailabilityAsync(
        Guid id, int quantity, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);

        if (product is null) return null;

        return (product.Stock >= quantity, product.Price, product.Name);
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Description, p.Price,
        p.Stock, p.Category, p.ImageUrl, p.CreatedAt);
}