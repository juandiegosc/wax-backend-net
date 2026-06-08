using Domain.Entities;

namespace Domain.OrderAggregate;

public class Order : BaseEntity
{
    public required string BuyerEmail { get; set; }
    public List<OrderItem> OrderItems { get; set; } = [];
    public long Subtotal { get; set; }
    public long DeliveryFee { get; set; }
    public long Discount { get; set; }
    public required string PaymentIntentId { get; set; }
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
    public required PaymentSummary PaymentSummary { get; set; }

    //Navigation props
    public required string BillingAddressId { get; set; }
    public required BillingAddress BillingAddress { get; set; }

    public long GetTotal()
    {
        return Subtotal + DeliveryFee - Discount;
    }
}
