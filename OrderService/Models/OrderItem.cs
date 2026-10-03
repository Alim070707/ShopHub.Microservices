namespace OrderService.Models;

/// <summary>
/// Позиция заказа — СНИМОК товара на момент покупки.
/// Хранит productId как ссылку, но name и price — копии.
/// Это гарантирует: даже если товар изменится, заказ остаётся верным.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }

    /// <summary>ID товара в CatalogService (не FK — другая БД).</summary>
    public Guid ProductId { get; set; }

    /// <summary>Название на момент заказа (snapshot).</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Цена на момент заказа (snapshot).</summary>
    public decimal PriceAtPurchase { get; set; }

    public int Quantity { get; set; }

    public Order Order { get; set; } = null!;
}