using MailKit.Net.Smtp;
using MimeKit;

namespace NotificationService.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>
/// Сервис отправки email через SMTP (MailKit).
/// Используется в production.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("ShopHub", _config["Smtp:From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _config["Smtp:Host"],
            int.Parse(_config["Smtp:Port"] ?? "587"),
            MailKit.Security.SecureSocketOptions.StartTls,
            ct);

        await client.AuthenticateAsync(_config["Smtp:Username"], _config["Smtp:Password"], ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email отправлен на {To}: '{Subject}'", to, subject);
    }
}

/// <summary>
/// Заглушка для разработки: не отправляет письмо, только пишет в лог.
/// Позволяет тестировать всю логику без реального SMTP.
/// </summary>
public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation(
            """
            ══════════════════════════════════
            📧 FAKE EMAIL (не отправлен реально)
            To: {To}
            Subject: {Subject}
            Body: {BodyPreview}...
            ══════════════════════════════════
            """,
            to, subject, htmlBody[..Math.Min(120, htmlBody.Length)]);

        return Task.CompletedTask;
    }
}