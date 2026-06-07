using Application.IntegrationEvents.OrderEvents;
using Application.Interfaces.Services;
using Application.Notifications.Requests;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Infrastructure.Messaging.Consumers.OrderConsumers;

public class OrderStatusChangedConsumer(
    ReadDbContext readContext,
    ILogger<OrderStatusChangedConsumer> logger,
    IEmailService emailService)
    : IConsumer<OrderStatusChangedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedIntegrationEvent> context)
    {
        var message = context.Message;

        var readModel = await readContext.Orders
            .FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);

        if (readModel == null)
        {
            logger.LogInformation("Order with id {OrderId} not found", message.OrderId);
            throw new InvalidOperationException("Order read model not found");
        }

        // Capture old status before the update
        var oldStatus = readModel.OrderStatus;

        readModel.OrderStatus = message.NewStatus;
        readModel.UpdatedAt = message.OccurredAt;
        readModel.LastSyncedAt = DateTime.UtcNow;

        readContext.Entry(readModel).State = EntityState.Modified;
        await readContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Order with id {OrderId} has been updated", message.OrderId);

        try
        {
            var emailRequest = new OrderStatusChangedEmailRequest
            {
                ToEmail = readModel.BuyerEmail,
                ToName = readModel.BillingName,
                OrderNumber = readModel.Id,
                OldStatus = oldStatus,
                NewStatus = message.NewStatus,
                Total = (int)(readModel.Total)
            };
            await emailService.SendAsync(emailRequest, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error sending {EmailType} email to {Email} for order {OrderId}",
                "OrderStatusChanged",
                readModel.BuyerEmail,
                readModel.Id);
        }
    }
}
