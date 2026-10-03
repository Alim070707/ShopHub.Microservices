namespace OrderService.Models;

/// <summary>
/// Заказ покупателя.
/// Содержит снимок (snapshot) данных на момент оформления.
/// ID пользователя — ссылка на IdentityService, не внешний ключ.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID пользователя из IdentityService (не FK — другая БД).</summary>
    public Guid UserId { get; set; }

    /// <summary>Snapshot email на момент заказа.</summary>
    public string UserEmail { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    public string? Comment { get; set; }

    // Snapshot адреса — копируем, не ссылаемся
    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingZipCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = [];
}

/// <summary>Статусы жизненного цикла заказа.</summary>
public enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}