using Application.Interfaces.Services;
using Application.Notifications.Requests;
using Infrastructure.Email;
using Infrastructure.Email.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace UnitTests.Infrastructure.Email;

public class ResendEmailServiceTests
{
    private readonly Mock<IResend> _resend = new();
    private readonly Mock<IEmailTemplateService> _templateService = new();
    private readonly Mock<ILogger<ResendEmailService>> _logger = new();

    private static readonly EmailSettings _settings = new()
    {
        ApiToken = "test-token",
        FromEmail = "from@example.com",
        FromName = "WAX Store",
        BaseUrl = "https://app.example.com"
    };

    private ResendEmailService CreateService()
    {
        var options = Options.Create(_settings);
        return new ResendEmailService(_resend.Object, options, _templateService.Object, _logger.Object);
    }

    private static AccountConfirmationEmailRequest BuildRequest() =>
        new()
        {
            ToEmail = "recipient@example.com",
            ToName = "Test User",
            ConfirmationLink = "https://confirm"
        };

    private static ResendResponse<Guid> BuildFailResponse()
    {
        var limits = new ResendRateLimit();
        var ex = new ResendException(null, ErrorType.MissingRequiredField, "failed", limits);
        return new ResendResponse<Guid>(ex, limits);
    }

    private static ResendResponse<Guid> BuildSuccessResponse()
    {
        var limits = new ResendRateLimit();
        return new ResendResponse<Guid>(Guid.NewGuid(), limits);
    }

    [Fact]
    public async Task SendAsync_WhenResendReturnsFalseSuccess_LogsErrorAndDoesNotLogInfo()
    {
        var service = CreateService();
        var request = BuildRequest();

        _templateService
            .Setup(t => t.Render(request))
            .Returns(("Subject", "<p>Body</p>"));

        _resend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildFailResponse());

        await service.SendAsync(request);

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error enviando email")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("enviado")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_WhenResendReturnsTrueSuccess_LogsInfoAndDoesNotLogError()
    {
        var service = CreateService();
        var request = BuildRequest();

        _templateService
            .Setup(t => t.Render(request))
            .Returns(("Subject", "<p>Body</p>"));

        _resend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSuccessResponse());

        await service.SendAsync(request);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("enviado")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
