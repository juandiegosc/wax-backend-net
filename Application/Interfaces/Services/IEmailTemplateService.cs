using Application.Notifications.Requests;

namespace Application.Interfaces.Services;

public interface IEmailTemplateService
{
    (string Subject, string HtmlBody) Render(EmailRequest request);
}
