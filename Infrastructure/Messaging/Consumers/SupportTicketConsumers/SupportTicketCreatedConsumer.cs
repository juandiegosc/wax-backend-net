using Application.IntegrationEvents.SupportTicketEvents;
using Application.Interfaces.Services;
using Application.Notifications.Requests;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.ReadModels;

namespace Infrastructure.Messaging.Consumers.SupportTicketConsumers;

public class SupportTicketCreatedConsumer(
    ReadDbContext readContext,
    ILogger<SupportTicketCreatedConsumer> logger,
    IEmailService emailService)
    : IConsumer<SupportTicketCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SupportTicketCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        var alreadyExists = await readContext.SupportTickets
            .AnyAsync(t => t.Id == message.TicketId, context.CancellationToken);

        if (alreadyExists)
        {
            logger.LogWarning("SupportTicket with id {TicketId} has already been added", message.TicketId);
            return;
        }

        var readModel = new SupportTicketReadModel
        {
            Id = message.TicketId,
            UserId = message.UserId,
            UserEmail = message.UserEmail,
            UserFullName = message.UserFullName,
            OrderId = message.OrderId,
            Category = message.Category,
            Status = message.Status,
            Subject = message.Subject,
            Description = message.Description,
            CreatedAt = message.OccurredAt,
            LastSyncedAt = DateTime.UtcNow
        };

        readContext.SupportTickets.Add(readModel);
        await readContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("SupportTicket with id {TicketId} has been added", message.TicketId);

        try
        {
            var emailRequest = new SupportTicketCreatedEmailRequest
            {
                ToEmail = message.UserEmail,
                ToName = message.UserFullName,
                OrderNumber = message.TicketId,
                Subject = message.Subject
            };
            await emailService.SendAsync(emailRequest, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error sending {EmailType} email to {Email} for ticket {TicketId}",
                "SupportTicketCreated",
                message.UserEmail,
                message.TicketId);
        }
    }
}
