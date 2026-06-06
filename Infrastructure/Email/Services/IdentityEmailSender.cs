using Application.Interfaces.Services;
using Application.Notifications.Requests;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email.Services;

public class IdentityEmailSender(IEmailService emailService, IOptions<EmailSettings> settings) : IEmailSender<User>
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var request = new AccountConfirmationEmailRequest
        {
            ToEmail = email,
            ToName = user.UserName ?? email,
            ConfirmationLink = confirmationLink
        };
        await emailService.SendAsync(request);
    }

    public async Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var request = new PasswordResetEmailRequest
        {
            ToEmail = email,
            ToName = user.UserName ?? email,
            ResetCode = resetCode,
            ResetLink = $"{_settings.BaseUrl}/reset-password?code={Uri.EscapeDataString(resetCode)}"
        };
        await emailService.SendAsync(request);
    }

    public async Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var request = new PasswordResetEmailRequest
        {
            ToEmail = email,
            ToName = user.UserName ?? email,
            ResetCode = "",
            ResetLink = resetLink
        };
        await emailService.SendAsync(request);
    }
}

