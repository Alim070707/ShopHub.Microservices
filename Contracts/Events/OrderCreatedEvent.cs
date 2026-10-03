namespace Contracts.Events;

/// <summary>
/// Событие создания заказа.
/// Публикует OrderService, потребляет NotificationService.
/// Это «договор» между сервисами — не менять поля без версионирования.
/// </summary>
public record OrderCreatedEvent
{
    /// <summary>Уникальный ID события — для идемпотентности.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public List<OrderItemEvent> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record OrderItemEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}