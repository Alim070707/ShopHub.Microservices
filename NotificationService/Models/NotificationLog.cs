namespace NotificationService.Models;

/// <summary>
/// Журнал уведомлений. Каждая запись — попытка отправки письма.
/// Уникальный индекс по EventId — гарантирует идемпотентность обработки.
/// </summary>
public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID события из RabbitMQ — для идемпотентности.</summary>
    public Guid EventId { get; set; }

    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Тип уведомления: ORDER_CREATED, PAYMENT_DONE, ...</summary>
    public string Type { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Статус: SENT / FAILED.</summary>
    public string Status { get; set; } = string.Empty;

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}