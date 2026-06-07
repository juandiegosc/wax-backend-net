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

public class SupportTicketUpdatedConsumerTests
{
    private static ReadDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReadDbContext(options);
    }

    private static ILogger<SupportTicketUpdatedConsumer> CreateLogger()
    {
        var logger = new Mock<ILogger<SupportTicketUpdatedConsumer>>();
        return logger.Object;
    }

    private static SupportTicketReadModel CreateTicketReadModel(string ticketId) => new()
    {
        Id = ticketId,
        UserId = "user-1",
        UserEmail = "user@test.com",
        UserFullName = "Test User",
        OrderId = "order-1",
        Category = "PaymentIssue",
        Status = "Open",
        Subject = "Original Subject",
        Description = "Original Description",
        CreatedAt = DateTime.UtcNow,
        LastSyncedAt = DateTime.UtcNow.AddMinutes(-5)
    };

    [Fact]
    public async Task Consume_WhenTicketExists_UpdatesFields()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        context.SupportTickets.Add(CreateTicketReadModel(ticketId));
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var consumer = new SupportTicketUpdatedConsumer(context, logger);
        var @event = new SupportTicketUpdatedIntegrationEvent
        {
            TicketId = ticketId,
            Category = "OrderIssue",
            Status = "InProgress",
            Subject = "Updated Subject",
            Description = "Updated Description"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketUpdatedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        ticket.Should().NotBeNull();
        ticket!.Category.Should().Be("OrderIssue");
        ticket.Status.Should().Be("InProgress");
        ticket.Subject.Should().Be("Updated Subject");
        ticket.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Consume_WhenTicketNotFound_ThrowsException()
    {
        using var context = CreateInMemoryContext();
        var logger = CreateLogger();
        var consumer = new SupportTicketUpdatedConsumer(context, logger);
        var @event = new SupportTicketUpdatedIntegrationEvent
        {
            TicketId = "non-existent",
            Category = "Other",
            Status = "Closed",
            Subject = "Subject",
            Description = "Description"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketUpdatedIntegrationEvent>>();
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
        var consumer = new SupportTicketUpdatedConsumer(context, logger);
        var eventTime = DateTime.UtcNow;
        var @event = new SupportTicketUpdatedIntegrationEvent
        {
            TicketId = ticketId,
            Category = "Other",
            Status = "Closed",
            Subject = "Subject",
            Description = "Description",
            OccurredAt = eventTime
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketUpdatedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        ticket!.UpdatedAt.Should().BeCloseTo(eventTime, TimeSpan.FromSeconds(1));
        ticket.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Consume_DoesNotModifyUserFields()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        context.SupportTickets.Add(CreateTicketReadModel(ticketId));
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var consumer = new SupportTicketUpdatedConsumer(context, logger);
        var @event = new SupportTicketUpdatedIntegrationEvent
        {
            TicketId = ticketId,
            Category = "Other",
            Status = "Closed",
            Subject = "New Subject",
            Description = "New Description"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketUpdatedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        ticket!.UserId.Should().Be("user-1");
        ticket.UserEmail.Should().Be("user@test.com");
        ticket.UserFullName.Should().Be("Test User");
    }

    // ── NEW: 5.9 anti-duplicate: SupportTicketUpdatedConsumer MUST NOT send email ──

    [Fact]
    public async Task Consume_NeverInvokesEmailService()
    {
        using var context = CreateInMemoryContext();
        var ticketId = Guid.NewGuid().ToString();
        context.SupportTickets.Add(CreateTicketReadModel(ticketId));
        await context.SaveChangesAsync();

        var logger = CreateLogger();
        var emailService = new Mock<IEmailService>();
        var consumer = new SupportTicketUpdatedConsumer(context, logger);

        var @event = new SupportTicketUpdatedIntegrationEvent
        {
            TicketId = ticketId,
            Category = "Other",
            Status = "InProgress",
            Subject = "New Subject",
            Description = "New Description"
        };

        var contextMock = new Mock<ConsumeContext<SupportTicketUpdatedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(@event);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        emailService.Verify(
            e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
