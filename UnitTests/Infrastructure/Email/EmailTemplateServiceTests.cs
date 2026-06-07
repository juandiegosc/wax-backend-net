using System.Reflection;
using Application.Notifications;
using Application.Notifications.Requests;
using Infrastructure.Email.EmailTemplates;

namespace UnitTests.Infrastructure.Email;

file record UnknownEmailRequest : EmailRequest
{
    public override EmailType EmailType => (EmailType)999;
}

public class EmailTemplateServiceTests
{
    private readonly EmailTemplateService _service = new();

    // ── 1.1a: Six embedded resources are present ────────────────────────────

    [Fact]
    public void Assembly_ContainsAllSixExpectedHtmlResources()
    {
        var assembly = typeof(EmailTemplateService).Assembly;
        var names = assembly.GetManifestResourceNames();

        var expected = new[]
        {
            "Infrastructure.Email.EmailTemplates.Templates.AccountConfirmation.html",
            "Infrastructure.Email.EmailTemplates.Templates.PasswordReset.html",
            "Infrastructure.Email.EmailTemplates.Templates.OrderStatusChanged.html",
            "Infrastructure.Email.EmailTemplates.Templates.PaymentConfirmed.html",
            "Infrastructure.Email.EmailTemplates.Templates.SupportTicketCreated.html",
            "Infrastructure.Email.EmailTemplates.Templates.SupportTicketUpdated.html"
        };

        foreach (var resourceName in expected)
        {
            names.Should().Contain(resourceName, because: $"resource '{resourceName}' must be embedded");
        }
    }

    // ── 1.1b: Render substitutes placeholder ToName ─────────────────────────

    [Fact]
    public void Render_AccountConfirmation_SubstitutesToName()
    {
        var request = new AccountConfirmationEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "John",
            ConfirmationLink = "https://confirm"
        };

        var (subject, body) = _service.Render(request);

        subject.Should().NotBeNullOrEmpty();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("John");
        body.Should().NotContain("{{ToName}}");
    }

    // ── 1.1c: HtmlEncode applied to user-sourced fields ─────────────────────

    [Fact]
    public void Render_WhenToNameContainsScriptTag_HtmlEncodesIt()
    {
        var request = new AccountConfirmationEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "<script>alert(1)</script>",
            ConfirmationLink = "https://confirm"
        };

        var (_, body) = _service.Render(request);

        body.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
        body.Should().NotContain("<script>");
    }

    // ── 1.1d: Raw URLs are NOT Html-encoded ─────────────────────────────────

    [Fact]
    public void Render_WhenConfirmationLinkContainsAmpersand_DoesNotEncodeIt()
    {
        var request = new AccountConfirmationEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "Maria",
            ConfirmationLink = "https://app.example.com/confirm?userId=1&token=abc"
        };

        var (_, body) = _service.Render(request);

        body.Should().Contain("https://app.example.com/confirm?userId=1&token=abc");
        body.Should().NotContain("&amp;token");
    }

    // ── 1.1e: Unknown request type throws with resource name in message ──────

    [Fact]
    public void Render_ForUnknownEmailType_ThrowsWithDescriptiveMessage()
    {
        var request = new UnknownEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "Unknown"
        };

        var act = () => _service.Render(request);

        act.Should().Throw<ArgumentException>();
    }

    // ── 1.1f: Repeat for all six request types ───────────────────────────────

    [Fact]
    public void Render_PasswordReset_SubjecAndPlaceholders()
    {
        var request = new PasswordResetEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "Alice",
            ResetCode = "CODE123",
            ResetLink = "https://reset-link"
        };

        var (subject, body) = _service.Render(request);

        subject.Should().NotBeNullOrEmpty();
        body.Should().Contain("Alice");
        body.Should().Contain("CODE123");
        body.Should().Contain("https://reset-link");
        body.Should().NotContain("{{ToName}}");
        body.Should().NotContain("{{ResetCode}}");
        body.Should().NotContain("{{ResetLink}}");
    }

    [Fact]
    public void Render_OrderStatusChanged_SubjectAndPlaceholders()
    {
        var request = new OrderStatusChangedEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "Bob",
            OrderNumber = "ORDER-12345678",
            OldStatus = "Pending",
            NewStatus = "Approved",
            Total = 10000
        };

        var (subject, body) = _service.Render(request);

        subject.Should().NotBeNullOrEmpty();
        body.Should().Contain("Bob");
        body.Should().Contain("Pending");
        body.Should().Contain("Approved");
        body.Should().NotContain("{{ToName}}");
        body.Should().NotContain("{{OldStatus}}");
        body.Should().NotContain("{{NewStatus}}");
    }

    [Fact]
    public void Render_PaymentConfirmed_SubjectAndPlaceholders()
    {
        var request = new PaymentConfirmedEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "Carol",
            OrderNumber = "ORDER-12345678",
            TotalAmount = 25000
        };

        var (subject, body) = _service.Render(request);

        subject.Should().NotBeNullOrEmpty();
        body.Should().Contain("Carol");
        body.Should().NotContain("{{ToName}}");
        body.Should().NotContain("{{OrderNumber}}");
        body.Should().NotContain("{{TotalAmount}}");
    }

    [Fact]
    public void Render_SupportTicketCreated_SubjectAndPlaceholders()
    {
        var request = new SupportTicketCreatedEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "Dave",
            OrderNumber = "TICKET-12345678",
            Subject = "Product is broken"
        };

        var (subject, body) = _service.Render(request);

        subject.Should().NotBeNullOrEmpty();
        body.Should().Contain("Dave");
        body.Should().Contain("Product is broken");
        body.Should().NotContain("{{Subject}}");
    }

    [Fact]
    public void Render_SupportTicketUpdated_SubjectAndPlaceholders()
    {
        var request = new SupportTicketUpdatedEmailRequest
        {
            ToEmail = "user@example.com",
            ToName = "Eve",
            OrderNumber = "TICKET-12345678",
            Subject = "Follow up issue",
            NewStatus = "InProgress"
        };

        var (subject, body) = _service.Render(request);

        subject.Should().NotBeNullOrEmpty();
        body.Should().Contain("Eve");
        body.Should().Contain("Follow up issue");
        body.Should().Contain("InProgress");
        body.Should().NotContain("{{NewStatus}}");
    }
}
