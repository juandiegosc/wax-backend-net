using System.Globalization;
using System.Text.Json;
using Application.Core.Validations;
using Application.IntegrationEvents.BillingEvents;
using Application.IntegrationEvents.OrderEvents;
using Application.IntegrationEvents.ProductEvents;
using Application.Interfaces.Publish;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Notifications.Requests;
using Application.Payment.Events;
using Domain.OrderAggregate;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Payment.Commands;

public class HandleStripeWebhookCommandHandler(
    IPaymentService paymentService,
    IOrderRepository orderRepository,
    IBasketRepository basketRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<HandleStripeWebhookCommandHandler> logger,
    IEmailService emailService)
    : IRequestHandler<HandleStripeWebhookCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var stripeEvent = paymentService.ConstructStripeEvent(request.Payload, request.Signature);

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                await HandlePaymentSucceeded(stripeEvent, cancellationToken);
            }
            else
            {
                await HandlePaymentFailed(stripeEvent, cancellationToken);
            }

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling Stripe webhook");
            return Result<Unit>.Failure("Error handling Stripe webhook");
        }
    }

    #region Private Methods
    private async Task HandlePaymentFailed(StripeEventResult stripeEvent, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByPaymentIntentIdAsync(stripeEvent.IntentId, cancellationToken);
        if (order is null)
            throw new InvalidOperationException("Order not found for payment intent: " + stripeEvent.IntentId);

        foreach (var item in order.OrderItems)
        {
            var productItem = await productRepository.GetByIdAsync(item.ItemOrdered.ProductId, cancellationToken);
            if (productItem is null) continue;

            productItem.QuantityInStock += item.Quantity;

            await eventPublisher.PublishEventAsync(new ProductStockChangedIntegrationEvent
            {
                ProductId = productItem.Id,
                NewQuantity = productItem.QuantityInStock
            }, cancellationToken);
        }

        order.OrderStatus = OrderStatus.PaymentFailed;

        await eventPublisher.PublishEventAsync(new OrderStatusChangedIntegrationEvent
        {
            OrderId = order.Id,
            NewStatus = order.OrderStatus.ToString()
        }, cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
    }

    private async Task HandlePaymentSucceeded(StripeEventResult stripeEvent, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByPaymentIntentIdAsync(stripeEvent.IntentId, cancellationToken);
        if (order is null)
        {
            Result<Unit>.Failure("Order not found", 404);
            return;
        }

        order.OrderStatus = order.GetTotal() != stripeEvent.Amount ? OrderStatus.PaymentMismatch : OrderStatus.PaymentRecieved;

        var basket = await basketRepository.GetBasketWithItemsAsync(
            order.PaymentIntentId, cancellationToken);

        if (basket != null) basketRepository.Remove(basket);

        await eventPublisher.PublishEventAsync(new OrderStatusChangedIntegrationEvent
        {
            OrderId = order.Id,
            NewStatus = order.OrderStatus.ToString()
        }, cancellationToken);

        if (order.OrderStatus == OrderStatus.PaymentRecieved)
        {
            await eventPublisher.PublishEventAsync(new OrderBillingRequestedIntegrationEvent
            {
                OrderId = order.Id,
                BuyerEmail = order.BuyerEmail,
                Subtotal = order.Subtotal,
                DeliveryFee = order.DeliveryFee,
                Total = order.GetTotal(),
                PaymentIntentId = order.PaymentIntentId,
                BillingName = order.BillingAddress?.Name,
                BillingLine1 = order.BillingAddress?.Line1,
                BillingLine2 = order.BillingAddress?.Line2,
                BillingCity = order.BillingAddress?.City,
                BillingState = order.BillingAddress?.State,
                BillingPostalCode = order.BillingAddress?.PostalCode,
                BillingCountry = order.BillingAddress?.Country,
                OrderItems = JsonSerializer.Serialize(order.OrderItems.Select(item => new
                {
                    item.ItemOrdered.Name,
                    item.ItemOrdered.ProductId,
                    item.Price,
                    item.Quantity
                })),
                OccurredAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        try
        {
            var emailRequest = new PaymentConfirmedEmailRequest
            {
                ToEmail = order.BuyerEmail,
                ToName = order.BillingAddress?.Name ?? order.BuyerEmail,
                OrderNumber = order.Id,
                TotalAmount = order.GetTotal()
            };
            await emailService.SendAsync(emailRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send PaymentConfirmed email for order {OrderId}",
                order.Id);
        }
    }
    #endregion
}
