using Application.IntegrationEvents.SupportTicketEvents;
using Application.Interfaces.Services;
using Application.Notifications.Requests;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Infrastructure.Messaging.Consumers.SupportTicketConsumers;

public class SupportTicketStatusChangedConsumer(
    ReadDbContext readContext,
    ILogger<SupportTicketStatusChangedConsumer> logger,
    IEmailService emailService)
    : IConsumer<SupportTicketStatusChangedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SupportTicketStatusChangedIntegrationEvent> context)
    {
        var message = context.Message;

        var readModel = await readContext.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == message.TicketId, context.CancellationToken);

        if (readModel is null)
        {
            logger.LogWarning("SupportTicket read model not found: TicketId={TicketId}", message.TicketId);
            throw new InvalidOperationException($"SupportTicket read model not found: {message.TicketId}");
        }

        readModel.Status = message.NewStatus;
        readModel.UpdatedAt = message.OccurredAt;
        readModel.LastSyncedAt = DateTime.UtcNow;

        readContext.Entry(readModel).State = EntityState.Modified;
        await readContext.SaveChangesAsync(context.CancellationToken);

        try
        {
            var emailRequest = new SupportTicketUpdatedEmailRequest
            {
                ToEmail = readModel.UserEmail,
                ToName = readModel.UserFullName,
                OrderNumber = readModel.Id,
                Subject = readModel.Subject,
                NewStatus = message.NewStatus
            };
            await emailService.SendAsync(emailRequest, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error sending {EmailType} email to {Email} for ticket {TicketId}",
                "SupportTicketUpdated",
                readModel.UserEmail,
                readModel.Id);
        }
    }
}
