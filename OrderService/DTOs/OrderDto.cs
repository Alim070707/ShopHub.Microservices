namespace OrderService.DTOs;

/// <summary>DTO для отдачи данных о заказе клиенту.</summary>
public record OrderDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string Status,
    decimal TotalAmount,
    string? Comment,
    DateTime CreatedAt,
    List<OrderItemDto> Items
);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal PriceAtPurchase,
    int Quantity
);