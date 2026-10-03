using Microsoft.EntityFrameworkCore;
using MassTransit;
using Contracts.Events;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;

namespace OrderService.Services;

/// <summary>
/// Сервис управления заказами.
/// Оркестрирует: валидацию, вызов CatalogService, сохранение заказа,
/// публикацию события OrderCreated в RabbitMQ.
/// </summary>
public class OrderManagementService
{
    private readonly OrderDbContext _db;
    private readonly ICatalogServiceClient _catalogClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderManagementService> _logger;

    public OrderManagementService(
        OrderDbContext db,
        ICatalogServiceClient catalogClient,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderManagementService> logger)
    {
        _db = db;
        _catalogClient = catalogClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Создать новый заказ.
    /// Шаги:
    ///   1. Проверить наличие каждого товара в CatalogService
    ///   2. Создать заказ со снимком данных (snapshot)
    ///   3. Сохранить в БД
    ///   4. Опубликовать событие OrderCreated в RabbitMQ
    /// </summary>
    public async Task<OrderDto> CreateAsync(
        CreateOrderDto dto, Guid userId, string userEmail, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Создание заказа для пользователя {UserId}, товаров: {Count}",
            userId, dto.Items.Count);

        // Шаг 1: Проверяем наличие каждого товара в CatalogService
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var item in dto.Items)
        {
            var availability = await _catalogClient.CheckAvailabilityAsync(
                item.ProductId, item.Quantity, ct);

            if (availability is null)
                throw new KeyNotFoundException($"Товар {item.ProductId} не найден в каталоге");

            if (!availability.InStock)
                throw new InvalidOperationException(
                    $"Товар '{availability.ProductName}' отсутствует на складе в нужном количестве");

            // Шаг 2: Создаём снимок — сохраняем данные на момент покупки
            var orderItem = new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = availability.ProductName,
                PriceAtPurchase = availability.CurrentPrice,
                Quantity = item.Quantity
            };

            orderItems.Add(orderItem);
            totalAmount += orderItem.PriceAtPurchase * orderItem.Quantity;
        }

        // Шаг 3: Сохраняем заказ в БД
        var order = new Order
        {
            UserId = userId,
            UserEmail = userEmail,
            TotalAmount = totalAmount,
            Comment = dto.Comment,
            ShippingStreet = dto.ShippingAddress.Street,
            ShippingCity = dto.ShippingAddress.City,
            ShippingZipCode = dto.ShippingAddress.ZipCode,
            Items = orderItems
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Заказ {OrderId} создан, сумма: {Total}", order.Id, totalAmount);

        // Шаг 4: Публикуем событие в RabbitMQ
        await _publishEndpoint.Publish(new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = userId,
            UserEmail = userEmail,
            TotalAmount = totalAmount,
            Items = orderItems.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Price = i.PriceAtPurchase
            }).ToList(),
            CreatedAt = order.CreatedAt
        }, ct);

        _logger.LogInformation(
            "Событие OrderCreated опубликовано для заказа {OrderId}", order.Id);

        return ToDto(order);
    }

    /// <summary>
    /// Получить список заказов пользователя.
    /// </summary>
    public async Task<List<OrderDto>> GetUserOrdersAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => ToDto(o))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Получить заказ по ID.
    /// </summary>
    public async Task<OrderDto?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order is null ? null : ToDto(order);
    }

    /// <summary>
    /// Преобразование Order → OrderDto.
    /// </summary>
    private static OrderDto ToDto(Order o) => new(
        o.Id,
        o.UserId,
        o.UserEmail,
        o.Status.ToString(),
        o.TotalAmount,
        o.Comment,
        o.CreatedAt,
        o.Items.Select(i => new OrderItemDto(
            i.ProductId,
            i.ProductName,
            i.PriceAtPurchase,
            i.Quantity)).ToList()
    );
}