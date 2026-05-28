using System.Text.Json;
using Application.Core.Validations;
using Application.IntegrationEvents.OrderEvents;
using Application.IntegrationEvents.ProductEvents;
using Application.Basket.Interfaces;
using Application.Interfaces.Services;
using Application.Interfaces.Publish;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Orders.DTOs;
using Application.Orders.Extensions;
using Domain.Entities;
using Domain.Enumerators;
using Domain.OrderAggregate;
using Domain.ProductAggregate;
using MediatR;

namespace Application.Orders.Commands;

public class CreateOrderCommandHandler(
    IBasketRepository basketRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IUserAccessor userAccessor,
    IEventPublisher eventPublisher,
    IBasketProvider basketProvider)
    : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var basket = await basketRepository.GetBasketWithItemsAsync(request.BasketId, cancellationToken);
        var user = await userAccessor.GetUserWithBillingAddressAsync();

        if (user == null) return Result<OrderDto>.Failure("User not found");
        var role = await userAccessor.GetUserRolesAsync();
        
        if (!role.Contains(Roles.Registered))
            return Result<OrderDto>.Failure("Only registered users can place orders");

        if (basket == null ||
            basket.Items.Count == 0 ||
            string.IsNullOrEmpty(basket.PaymentIntentId))
            return Result<OrderDto>.Failure("Basket not found or is empty");

        var items = CreateOrderItems(basket.Items);

        if (items == null) return Result<OrderDto>.Failure("One or more items in the basket are out of stock");

        var subtotal = items.Sum(x => x.Price * x.Quantity);
        var deliveryFee = CalculateDeliveryFee(subtotal);
        var hasCustomProduct = basket.Items.Any(i => i.Product is CustomProduct);

        var order = await orderRepository.GetByPaymentIntentIdAsync(basket.PaymentIntentId, cancellationToken);

        if (order == null)
        {
            order = new Order
            {
                BuyerEmail = user.Email ?? string.Empty,
                BillingAddress = user.BillingAddress!,
                BillingAddressId = user.BillingAddressId!,
                OrderItems = items,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                OrderStatus = hasCustomProduct ? OrderStatus.CustomOrder : OrderStatus.Pending,
                PaymentIntentId = basket.PaymentIntentId,
                PaymentSummary = request.OrderDto.PaymentSummary
            };

            orderRepository.Add(order);
        }
        else
        {
            order.OrderItems = items;
        }
        
        await eventPublisher.PublishEventAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            BuyerEmail = order.BuyerEmail,
            OrderStatus = order.OrderStatus.ToString(),
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            Total = order.GetTotal(),
            BillingName = order.BillingAddress.Name,
            BillingLine1 = order.BillingAddress.Line1,
            BillingLine2 = order.BillingAddress.Line2,
            BillingCity = order.BillingAddress.City,
            BillingState = order.BillingAddress.State,
            BillingPostalCode = order.BillingAddress.PostalCode,
            BillingCountry = order.BillingAddress.Country,
            PaymentLast4 = order.PaymentSummary.Last4,
            PaymentBrand = order.PaymentSummary.Brand,
            PaymentExpMonth = order.PaymentSummary.ExpMonth,
            PaymentExpYear = order.PaymentSummary.ExpYear,
            OrderItems = JsonSerializer.Serialize(order.OrderItems.Select(item => new
            {
                item.ItemOrdered.Name,
                item.ItemOrdered.ProductId,
                item.Price,
                item.Quantity
            })),
            PaymentIntentId = order.PaymentIntentId,
            UserId = user.Id,
            OccurredAt = DateTime.UtcNow
        }, cancellationToken);
        
        foreach (var catalog in basket.Items.Select(i => i.Product).OfType<CatalogProduct>())
        {
            await eventPublisher.PublishEventAsync(new ProductStockChangedIntegrationEvent
            {
                ProductId = catalog.Id,
                NewQuantity = catalog.QuantityInStock
            }, cancellationToken);
        }

        var result = await unitOfWork.CompleteAsync(cancellationToken);

        if (!result) return Result<OrderDto>.Failure("Failed to create order");

        basketProvider.DeleteBasketId();

        return Result<OrderDto>.Success(order.ToDto());
    }

    #region Private Methods

    private static List<OrderItem>? CreateOrderItems(List<BasketItem> items)
    {
        var orderItems = new List<OrderItem>();

        foreach (var item in items)
        {
            if (item.Product is CatalogProduct catalog)
            {
                if (catalog.QuantityInStock < item.Quantity) return null;
                catalog.QuantityInStock -= item.Quantity;
            }

            orderItems.Add(new OrderItem
            {
                ItemOrdered = new ProductOrderItem
                {
                    ProductId = item.Product.Id,
                    Name = item.Product.Name,
                },
                Price = item.Product.Price,
                Quantity = item.Quantity
            });
        }

        return orderItems;
    }

    private static long CalculateDeliveryFee(long subtotal)
    {
        return (long)(subtotal * 0.15m);
    }

    #endregion
}
