using Microsoft.EntityFrameworkCore;
using MassTransit;
using Contracts.Events;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Consumers;

/// <summary>
/// Обработчик события OrderCreated.
/// MassTransit автоматически создаст очередь и подпишется на exchange shop.order.created.
/// Метод Consume вызывается при получении каждого сообщения.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly NotificationDbContext _db;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IEmailService emailService,
        NotificationDbContext db,
        ILogger<OrderCreatedConsumer> logger)
    {
        _emailService = emailService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Обработать событие OrderCreated:
    ///   1. Проверить идемпотентность (не отправляли ли уже)
    ///   2. Сформировать HTML-письмо
    ///   3. Отправить покупателю
    ///   4. Записать в журнал
    /// </summary>
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "Получено событие OrderCreated: заказ {OrderId}, email {UserEmail}, сумма {Total}",
            evt.OrderId, evt.UserEmail, evt.TotalAmount);

        // Шаг 1: Проверка идемпотентности
        var alreadyProcessed = await _db.NotificationLogs
            .AnyAsync(l => l.EventId == evt.EventId, context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Событие {EventId} уже обработано — пропускаем", evt.EventId);
            return;
        }

        // Шаг 2: Формируем письмо
        var shortId = evt.OrderId.ToString()[..8].ToUpper();
        var subject = $"✅ Заказ #{shortId} успешно оформлен";
        var body = BuildEmailBody(evt, shortId);

        // Шаг 3: Готовим запись в журнал
        var log = new NotificationLog
        {
            EventId = evt.EventId,
            UserId = evt.UserId,
            UserEmail = evt.UserEmail,
            Type = "ORDER_CREATED",
            Subject = subject,
            Body = body
        };

        try
        {
            await _emailService.SendAsync(evt.UserEmail, subject, body, context.CancellationToken);

            log.Status = "SENT";
            log.SentAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Email отправлен: {UserEmail} (заказ {OrderId})",
                evt.UserEmail, evt.OrderId);
        }
        catch (Exception ex)
        {
            log.Status = "FAILED";
            log.Error = ex.Message;

            _logger.LogError(ex,
                "Ошибка отправки email для заказа {OrderId}", evt.OrderId);
        }

        // Шаг 4: Сохраняем в журнал
        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync(context.CancellationToken);
    }

    /// <summary>Строит HTML-тело письма с деталями заказа.</summary>
    private static string BuildEmailBody(OrderCreatedEvent evt, string shortId)
    {
        var items = string.Join("", evt.Items.Select(i =>
            $"<tr><td>{i.ProductName}</td><td>{i.Quantity} шт.</td><td>{i.Price:C}</td></tr>"));

        return $"""
            <html><body>
              <h2>Спасибо за заказ!</h2>
              <p>Ваш заказ <strong>#{shortId}</strong> успешно оформлен.</p>
              <table border="1" cellpadding="8">
                <thead><tr><th>Товар</th><th>Кол-во</th><th>Цена</th></tr></thead>
                <tbody>{items}</tbody>
              </table>
              <p><strong>Итого: {evt.TotalAmount:C}</strong></p>
              <p>Мы свяжемся с вами в ближайшее время.</p>
            </body></html>
            """;
    }
}