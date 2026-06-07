namespace Application.IntegrationEvents.BillingEvents;

public class OrderBillingRequestedIntegrationEvent
{
    public string OrderId { get; init; } = default!;
    public string BuyerEmail { get; init; } = default!;
    public long Subtotal { get; init; }
    public long DeliveryFee { get; init; }
    public long Total { get; init; }
    public string PaymentIntentId { get; init; } = default!;
    public string? BillingName { get; init; }
    public string? BillingLine1 { get; init; }
    public string? BillingLine2 { get; init; }
    public string? BillingCity { get; init; }
    public string? BillingState { get; init; }
    public string? BillingPostalCode { get; init; }
    public string? BillingCountry { get; init; }
    public string OrderItems { get; init; } = default!;
    public DateTime OccurredAt { get; init; }
}
