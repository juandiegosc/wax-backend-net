using System.Text.Json;
using Application.Billing.DTOs;
using Application.IntegrationEvents.BillingEvents;
using Application.Interfaces.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Consumers.BillingConsumers;

public class OrderBillingRequestedConsumer(
    IBillingFacade billingFacade,
    ILogger<OrderBillingRequestedConsumer> logger) : IConsumer<OrderBillingRequestedIntegrationEvent>
{
    private sealed record OrderItemLine(string Name, string ProductId, long Price, int Quantity);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task Consume(ConsumeContext<OrderBillingRequestedIntegrationEvent> context)
    {
        var evt = context.Message;

        var orderItems = JsonSerializer.Deserialize<List<OrderItemLine>>(evt.OrderItems, JsonOptions)
            ?? [];

        var legalName = evt.BillingName ?? evt.BuyerEmail;

        var address = evt.BillingLine1 is not null
            ? string.Join(" ",
                new[] { evt.BillingLine1, evt.BillingLine2, evt.BillingCity, evt.BillingState, evt.BillingCountry }
                    .Where(s => !string.IsNullOrWhiteSpace(s))).Trim()
            : null;
        
        var lines = orderItems.Select(item => new InvoiceLine(
            Code: item.ProductId,
            Description: item.Name,
            Quantity: item.Quantity,
            UnitPrice: item.Price / 100m,
            Tax: 0m)).ToList<InvoiceLine>();

        var request = new InvoiceRequest(
            OrderId: evt.OrderId,
            PaymentIntentId: evt.PaymentIntentId,
            Customer: new InvoiceCustomer(
                Identification: "9999999999",
                LegalName: legalName,
                Email: evt.BuyerEmail,
                Address: address),
            Items: lines,
            Payment: new InvoicePayment(
                Amount: evt.Total / 100m,
                Currency: "USD"));

        var result = await billingFacade.EmitInvoiceAsync(request, context.CancellationToken);

        if (!result.Success)
        {
            logger.LogWarning(
                "FacturaPlan invoice emission failed for order {OrderId}: {ErrorMessage}",
                evt.OrderId,
                result.ErrorMessage);
            throw new InvalidOperationException(result.ErrorMessage);
        }

        logger.LogInformation(
            "Invoice emitted successfully for order {OrderId}. ExternalInvoiceId: {ExternalInvoiceId}",
            evt.OrderId,
            result.ExternalInvoiceId);
    }
}
