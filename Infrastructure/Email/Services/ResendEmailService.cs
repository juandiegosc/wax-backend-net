using Application.Interfaces.Services;
using Application.Notifications.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Infrastructure.Email.Services;

public class ResendEmailService(
    IResend resend,
    IOptions<EmailSettings> settings,
    IEmailTemplateService templateService,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        var (subject, htmlBody) = templateService.Render(request);

        var message = new EmailMessage
        {
            From = $"{_settings.FromName} <{_settings.FromEmail}>",
            Subject = subject,
            HtmlBody = htmlBody
        };
        message.To.Add(request.ToEmail);

        var sendEmail = await resend.EmailSendAsync(message, cancellationToken);

        if (!sendEmail.Success)
        {
            logger.LogError(
                "Error enviando email {EmailType} a {Email}",
                request.EmailType,
                request.ToEmail);
            return;
        }

        logger.LogInformation(
            "Email {EmailType} enviado a {Email}",
            request.EmailType,
            request.ToEmail);
    }
}
