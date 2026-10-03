using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

/// <summary>DTO для создания заказа.</summary>
public record CreateOrderDto(
    [Required, MinLength(1, ErrorMessage = "Нужен хотя бы один товар")]
    List<CreateOrderItemDto> Items,

    [Required]
    ShippingAddressDto ShippingAddress,

    string? Comment
);

public record CreateOrderItemDto(
    [Required] Guid ProductId,
    [Required, Range(1, int.MaxValue)] int Quantity
);

public record ShippingAddressDto(
    [Required] string Street,
    [Required] string City,
    [Required] string ZipCode
);