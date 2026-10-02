namespace CatalogService.DTOs;

/// <summary>DTO для обновления существующего товара. Все поля опциональны.</summary>
public record UpdateProductDto(
    string? Name,
    string? Description,
    decimal? Price,
    int? Stock,
    string? Category,
    string? ImageUrl
);