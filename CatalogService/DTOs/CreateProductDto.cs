using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs;

/// <summary>
/// DTO для создания нового товара.
/// Клиент не может задать Id, IsActive, CreatedAt — это устанавливает сервер.
/// </summary>
public record CreateProductDto(
    [Required(ErrorMessage = "Название обязательно")]
    [MinLength(3), MaxLength(200)]
    string Name,

    string? Description,

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
    decimal Price,

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Остаток не может быть отрицательным")]
    int Stock,

    [Required(ErrorMessage = "Категория обязательна")]
    string Category,

    string? ImageUrl
);