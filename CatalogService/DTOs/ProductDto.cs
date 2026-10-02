namespace CatalogService.DTOs;

/// <summary>
/// DTO для отдачи данных о товаре клиентам.
/// Не содержит внутренних полей (IsActive, служебные даты).
/// </summary>
public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category,
    string? ImageUrl,
    DateTime CreatedAt
);