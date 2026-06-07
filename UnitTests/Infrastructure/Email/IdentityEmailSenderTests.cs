using Application.Interfaces.Services;
using Application.Notifications.Requests;
using Domain.Entities;
using Infrastructure.Email;
using Infrastructure.Email.Services;
using Microsoft.Extensions.Options;

namespace UnitTests.Infrastructure.Email;

public class IdentityEmailSenderTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly EmailSettings _settings = new()
    {
        ApiToken = "test-token",
        FromEmail = "from@example.com",
        FromName = "WAX Store",
        BaseUrl = "https://app.example.com"
    };

    private IdentityEmailSender CreateSender()
    {
        var options = Microsoft.Extensions.Options.Options.Create(_settings);
        return new IdentityEmailSender(_emailService.Object, options);
    }

    private static User CreateUser(string? userName = "user@example.com")
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            Email = "user@example.com"
        };
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_SendsAccountConfirmationEmailWithCorrectValues()
    {
        var sender = CreateSender();
        var user = CreateUser("testuser");
        AccountConfirmationEmailRequest? captured = null;

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailRequest, CancellationToken>((r, _) => captured = r as AccountConfirmationEmailRequest)
            .Returns(Task.CompletedTask);

        await sender.SendConfirmationLinkAsync(user, "user@example.com", "https://confirm?token=abc");

        _emailService.Verify(s => s.SendAsync(It.IsAny<AccountConfirmationEmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        captured.Should().NotBeNull();
        captured!.ToEmail.Should().Be("user@example.com");
        captured.ToName.Should().Be("testuser");
        captured.ConfirmationLink.Should().Be("https://confirm?token=abc");
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_WhenUserNameIsNull_UsesEmailAsName()
    {
        var sender = CreateSender();
        var user = CreateUser(null);
        AccountConfirmationEmailRequest? captured = null;

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailRequest, CancellationToken>((r, _) => captured = r as AccountConfirmationEmailRequest)
            .Returns(Task.CompletedTask);

        await sender.SendConfirmationLinkAsync(user, "fallback@example.com", "https://confirm");

        captured.Should().NotBeNull();
        captured!.ToName.Should().Be("fallback@example.com");
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_SendsPasswordResetEmailWithCode()
    {
        var sender = CreateSender();
        var user = CreateUser("resetuser");
        PasswordResetEmailRequest? captured = null;

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailRequest, CancellationToken>((r, _) => captured = r as PasswordResetEmailRequest)
            .Returns(Task.CompletedTask);

        await sender.SendPasswordResetCodeAsync(user, "user@example.com", "RESET123");

        _emailService.Verify(s => s.SendAsync(It.IsAny<PasswordResetEmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        captured.Should().NotBeNull();
        captured!.ResetCode.Should().Be("RESET123");
        captured.ToEmail.Should().Be("user@example.com");
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_BuildsResetLinkFromBaseUrl()
    {
        var sender = CreateSender();
        var user = CreateUser("resetuser");
        PasswordResetEmailRequest? captured = null;

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailRequest, CancellationToken>((r, _) => captured = r as PasswordResetEmailRequest)
            .Returns(Task.CompletedTask);

        await sender.SendPasswordResetCodeAsync(user, "user@example.com", "CODE456");

        captured.Should().NotBeNull();
        captured!.ResetLink.Should().Be("https://app.example.com/reset-password?code=CODE456");
    }

    // ── NEW: 7.1 URL escape tests ─────────────────────────────────────────────

    [Fact]
    public async Task SendPasswordResetCodeAsync_WhenResetCodeHasSpecialChars_EncodesInResetLink()
    {
        var sender = CreateSender();
        var user = CreateUser("resetuser");
        PasswordResetEmailRequest? captured = null;

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailRequest, CancellationToken>((r, _) => captured = r as PasswordResetEmailRequest)
            .Returns(Task.CompletedTask);

        await sender.SendPasswordResetCodeAsync(user, "user@example.com", "abc+/=def");

        captured.Should().NotBeNull();
        captured!.ResetLink.Should().Contain("abc%2B%2F%3Ddef");
        captured.ResetLink.Should().NotContain("abc+/=def");
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_DoesNotApplyAdditionalEscaping()
    {
        var sender = CreateSender();
        var user = CreateUser("linkuser");
        PasswordResetEmailRequest? captured = null;

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailRequest, CancellationToken>((r, _) => captured = r as PasswordResetEmailRequest)
            .Returns(Task.CompletedTask);

        const string prebuiltLink = "https://example.com/reset?code=already%2Fencoded";
        await sender.SendPasswordResetLinkAsync(user, "user@example.com", prebuiltLink);

        captured.Should().NotBeNull();
        captured!.ResetLink.Should().Be(prebuiltLink);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_SendsPasswordResetEmailWithLink()
    {
        var sender = CreateSender();
        var user = CreateUser("linkuser");
        PasswordResetEmailRequest? captured = null;

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<EmailRequest, CancellationToken>((r, _) => captured = r as PasswordResetEmailRequest)
            .Returns(Task.CompletedTask);

        await sender.SendPasswordResetLinkAsync(user, "user@example.com", "https://reset-link");

        _emailService.Verify(s => s.SendAsync(It.IsAny<PasswordResetEmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        captured.Should().NotBeNull();
        captured!.ResetLink.Should().Be("https://reset-link");
        captured.ResetCode.Should().Be(string.Empty);
    }
}
