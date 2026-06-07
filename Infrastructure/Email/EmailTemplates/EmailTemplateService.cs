using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Application.Interfaces.Services;
using Application.Notifications.Requests;

namespace Infrastructure.Email.EmailTemplates;

public class EmailTemplateService : IEmailTemplateService
{
    private static readonly Assembly _assembly = typeof(EmailTemplateService).Assembly;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public (string Subject, string HtmlBody) Render(EmailRequest request)
    {
        var (templateName, subject, escaped, raw) = BuildContext(request);
        var template = LoadTemplate(templateName);
        var body = ApplyPlaceholders(template, escaped, raw);
        return (subject, body);
    }

    private static (string templateName, string subject,
        Dictionary<string, string> escaped,
        Dictionary<string, string> raw) BuildContext(EmailRequest request) =>
        request switch
        {
            AccountConfirmationEmailRequest r => (
                "AccountConfirmation",
                "Confirma tu cuenta",
                new Dictionary<string, string>
                {
                    ["ToName"] = r.ToName
                },
                new Dictionary<string, string>
                {
                    ["ConfirmationLink"] = r.ConfirmationLink
                }),

            PasswordResetEmailRequest r => (
                "PasswordReset",
                "Restablecer contrasena",
                new Dictionary<string, string>
                {
                    ["ToName"] = r.ToName,
                    ["ResetCode"] = r.ResetCode
                },
                new Dictionary<string, string>
                {
                    ["ResetLink"] = r.ResetLink
                }),

            OrderStatusChangedEmailRequest r => (
                "OrderStatusChanged",
                $"Tu orden #{r.OrderNumber[..8]} ha cambiado de estado",
                new Dictionary<string, string>
                {
                    ["ToName"] = r.ToName,
                    ["OrderNumber"] = r.OrderNumber[..8],
                    ["OldStatus"] = r.OldStatus,
                    ["NewStatus"] = r.NewStatus,
                    // Total is in cents — divide by 100 before display
                    ["Total"] = (r.Total / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                },
                new Dictionary<string, string>()),

            PaymentConfirmedEmailRequest r => (
                "PaymentConfirmed",
                $"Pago confirmado - Orden #{r.OrderNumber[..8]}",
                new Dictionary<string, string>
                {
                    ["ToName"] = r.ToName,
                    ["OrderNumber"] = r.OrderNumber[..8],
                    // TotalAmount is in cents — divide by 100 before display
                    ["TotalAmount"] = (r.TotalAmount / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                },
                new Dictionary<string, string>()),

            SupportTicketCreatedEmailRequest r => (
                "SupportTicketCreated",
                $"Ticket de soporte creado - #{r.OrderNumber[..8]}",
                new Dictionary<string, string>
                {
                    ["ToName"] = r.ToName,
                    ["OrderNumber"] = r.OrderNumber[..8],
                    ["Subject"] = r.Subject
                },
                new Dictionary<string, string>()),

            SupportTicketUpdatedEmailRequest r => (
                "SupportTicketUpdated",
                $"Actualizacion de ticket - #{r.OrderNumber[..8]}",
                new Dictionary<string, string>
                {
                    ["ToName"] = r.ToName,
                    ["OrderNumber"] = r.OrderNumber[..8],
                    ["Subject"] = r.Subject,
                    ["NewStatus"] = r.NewStatus
                },
                new Dictionary<string, string>()),

            _ => throw new ArgumentException($"No template for {request.EmailType}")
        };

    private string LoadTemplate(string templateName)
    {
        return _cache.GetOrAdd(templateName, name =>
        {
            var resourceName = $"Infrastructure.Email.EmailTemplates.Templates.{name}.html";
            using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Email template resource '{resourceName}' not found in assembly");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });
    }

    private static string ApplyPlaceholders(string template,
        Dictionary<string, string> escaped,
        Dictionary<string, string> raw)
    {
        // Raw first (URLs) — no encoding
        foreach (var (key, value) in raw)
            template = template.Replace($"{{{{{key}}}}}", value);

        // Escaped second — HtmlEncode user-sourced values
        foreach (var (key, value) in escaped)
            template = template.Replace($"{{{{{key}}}}}", WebUtility.HtmlEncode(value));

        return template;
    }
}
