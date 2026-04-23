using Donately.Application.Common;
using Donately.Application.Common.Results;
using Donately.Application.Interfaces;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Donately.Infrastructure.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly EmailSenderOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailSenderOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result> SendPasswordResetEmailAsync(SendPasswordResetEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return new Error("Email.Empty", "Email не може бути порожнім");
        }

        if (string.IsNullOrWhiteSpace(_options.SendGridKey))
        {
            _logger.LogError("SendGrid key не налаштований.");
            return new Error("Email.Configuration", "Сервіс email тимчасово недоступний");
        }

        var message = new SendGridMessage
        {
            From = new EmailAddress(_options.FromAddress, _options.FromName),
            Subject = "Скидання пароля Donately",
            PlainTextContent = $"Щоб скинути пароль, відкрийте посилання: {request.ResetLink}",
            HtmlContent = $"<p>Ви надіслали запит на скидання пароля в Donately.</p><p>Натисніть <a href=\"{request.ResetLink}\">це посилання</a>, щоб встановити новий пароль.</p><p>Якщо ви не надсилали запит, просто проігноруйте цей лист.</p>"
        };

        message.AddTo(new EmailAddress(request.ToEmail));
        message.SetClickTracking(false, false);

        var client = new SendGridClient(_options.SendGridKey);
        var response = await client.SendEmailAsync(message, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Лист для скидання пароля відправлено на {Email}", request.ToEmail);
            return Success.Value;
        }

        _logger.LogError("SendGrid повернув помилку {StatusCode} для {Email}", response.StatusCode, request.ToEmail);
        return new Error("Email.SendFailed", "Не вдалося відправити лист для скидання пароля");
    }
}

