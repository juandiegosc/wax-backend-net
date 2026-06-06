using Application.IntegrationEvents.SupportTicketEvents;
using Application.Interfaces.Services;
using Application.Notifications.Requests;
using Infrastructure.Messaging.Consumers.SupportTicketConsumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.ReadModels;

namespace UnitTests.Infrastructure.Messaging.Consumers.SupportTicketConsumers;

public class SupportTicketStatusChangedConsumerTests
{
    private static ReadDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReadDbContext(options);
    }

    private static ILogger<SupportTicketStatusChangedConsumer> CreateLogger()
    {
        var logger = new Mock<ILogger<SupportTicketStatusChangedConsumer>>();
        return logger.Object;
    }

    private static SupportTicketReadModel CreateTicketReadModel(string ticketId, string status = "Open") => new()
    {
        Id = ticketId,
        UserId = "user-1",
        UserEmail = "user@test.com",
        UserFullName = "Test User",
        OrderId = "order-1",
        Category = "PaymentIssue",
        Status = status,
        Subject = "Subject",
        Description = "Description",
        CreatedAt = DateTime.UtcNow,
        LastSyncedAt = DateTime.UtcNow.AddMinutes(-5)
    };

    [Fact]
    public async Task Consume_WhenTicketExists_UpdatesStatus()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        context.SupportTickets.Add(CreateTicketReadModel(ticketId, "Open"));
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var emailService = new Mock<IEmailService>();
        var consumer = new SupportTicketStatusChangedConsumer(context, logger, emailService.Object);
        var @event = new SupportTicketStatusChangedIntegrationEvent
        {
            TicketId = ticketId,
            NewStatus = "Closed"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketStatusChangedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        ticket.Should().NotBeNull();
        ticket!.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task Consume_WhenTicketNotFound_ThrowsException()
    {
        using var context = CreateInMemoryContext();
        var logger = CreateLogger();
        var emailService = new Mock<IEmailService>();
        var consumer = new SupportTicketStatusChangedConsumer(context, logger, emailService.Object);
        var @event = new SupportTicketStatusChangedIntegrationEvent
        {
            TicketId = "non-existent",
            NewStatus = "Closed"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketStatusChangedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(contextMock.Object);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Consume_UpdatesTimestamps()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        context.SupportTickets.Add(CreateTicketReadModel(ticketId));
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var emailService = new Mock<IEmailService>();
        var consumer = new SupportTicketStatusChangedConsumer(context, logger, emailService.Object);
        var eventTime = DateTime.UtcNow;
        var @event = new SupportTicketStatusChangedIntegrationEvent
        {
            TicketId = ticketId,
            NewStatus = "InProgress",
            OccurredAt = eventTime
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketStatusChangedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        ticket!.UpdatedAt.Should().BeCloseTo(eventTime, TimeSpan.FromSeconds(1));
        ticket.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Consume_DoesNotModifyOtherFields()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        context.SupportTickets.Add(CreateTicketReadModel(ticketId));
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var emailService = new Mock<IEmailService>();
        var consumer = new SupportTicketStatusChangedConsumer(context, logger, emailService.Object);
        var @event = new SupportTicketStatusChangedIntegrationEvent
        {
            TicketId = ticketId,
            NewStatus = "Closed"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketStatusChangedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        ticket!.Category.Should().Be("PaymentIssue");
        ticket.Subject.Should().Be("Subject");
        ticket.Description.Should().Be("Description");
        ticket.UserId.Should().Be("user-1");
    }

    // ── NEW: 5.7 email wiring tests ───────────────────────────────────────────

    [Fact]
    public async Task Consume_WhenTicketStatusChanged_SendsEmailWithCorrectData()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        var ticket = new SupportTicketReadModel
        {
            Id = ticketId,
            UserId = "user-1",
            UserEmail = "ticket-owner@example.com",
            UserFullName = "Alice",
            OrderId = "order-1",
            Category = "PaymentIssue",
            Status = "Open",
            Subject = "Broken item",
            Description = "My item is broken",
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        context.SupportTickets.Add(ticket);
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var emailService = new Mock<IEmailService>();
        var consumer = new SupportTicketStatusChangedConsumer(context, logger, emailService.Object);

        var @event = new SupportTicketStatusChangedIntegrationEvent
        {
            TicketId = ticketId,
            NewStatus = "Resolved"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketStatusChangedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        emailService.Verify(
            e => e.SendAsync(
                It.Is<SupportTicketUpdatedEmailRequest>(r =>
                    r.ToEmail == "ticket-owner@example.com" &&
                    r.NewStatus == "Resolved"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenEmailFails_DoesNotPropagateExceptionAndUpdateIsPersisted()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        context.SupportTickets.Add(CreateTicketReadModel(ticketId, "Open"));
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var emailService = new Mock<IEmailService>();
        emailService
            .Setup(e => e.SendAsync(It.IsAny<SupportTicketUpdatedEmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Email failure"));

        var consumer = new SupportTicketStatusChangedConsumer(context, logger, emailService.Object);

        var @event = new SupportTicketStatusChangedIntegrationEvent
        {
            TicketId = ticketId,
            NewStatus = "Closed"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketStatusChangedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(contextMock.Object);

        await act.Should().NotThrowAsync();

        var updated = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        updated!.Status.Should().Be("Closed");
    }
}
