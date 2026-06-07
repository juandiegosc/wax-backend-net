using Application.IntegrationEvents.BillingEvents;
using Application.IntegrationEvents.OrderEvents;
using Application.IntegrationEvents.ProductEvents;
using Application.Interfaces.Publish;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Notifications.Requests;
using Application.Payment.Commands;
using Application.Payment.Events;
using Domain.OrderAggregate;
using Microsoft.Extensions.Logging;
using UnitTests.Helpers.Fixtures;
using DomainBasket = Domain.Entities.Basket;

namespace UnitTests.Application.Payment;

public class HandleStripeWebhookCommandHandlerTests
{
    private readonly Mock<IPaymentService> _paymentService = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IBasketRepository> _basketRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<ILogger<HandleStripeWebhookCommandHandler>> _logger = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly HandleStripeWebhookCommandHandler _handler;

    public HandleStripeWebhookCommandHandlerTests()
    {
        _handler = new HandleStripeWebhookCommandHandler(
            _paymentService.Object,
            _orderRepo.Object,
            _basketRepo.Object,
            _productRepo.Object,
            _unitOfWork.Object,
            _eventPublisher.Object,
            _logger.Object,
            _emailService.Object);
    }

    private static HandleStripeWebhookCommand CreateCommand() => new()
    {
        Payload = "test_payload",
        Signature = "test_signature"
    };

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_AndAmountMatches_SetsStatusToPaymentReceived()
    {
        var order = OrderFixtures.CreateOrder(subtotal: 5000, deliveryFee: 500);
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: order.GetTotal());

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatus.Should().Be(OrderStatus.PaymentRecieved);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_AndAmountMismatch_SetsStatusToPaymentMismatch()
    {
        var order = OrderFixtures.CreateOrder(subtotal: 5000, deliveryFee: 500);
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: 9999);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatus.Should().Be(OrderStatus.PaymentMismatch);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_AndBasketExists_RemovesBasket()
    {
        var order = OrderFixtures.CreateOrder();
        var basket = BasketFixtures.CreateBasketWithItems();
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: order.GetTotal());

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _basketRepo.Verify(r => r.Remove(basket), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_PublishesOrderStatusChangedEvent()
    {
        var order = OrderFixtures.CreateOrder();
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: order.GetTotal());

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _eventPublisher.Verify(
            e => e.PublishEventAsync(
                It.Is<OrderStatusChangedIntegrationEvent>(ev => ev.OrderId == order.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPaymentFailed_SetsStatusToPaymentFailed()
    {
        var product = ProductFixtures.CreateProduct(quantityInStock: 5);
        var orderItem = new OrderItem
        {
            ItemOrdered = new ProductOrderItem { ProductId = product.Id, Name = product.Name },
            Price = 1500,
            Quantity = 2
        };
        var order = OrderFixtures.CreateOrder(items: [orderItem]);

        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.payment_failed",
            Status: "failed",
            IntentId: order.PaymentIntentId,
            Amount: 0);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatus.Should().Be(OrderStatus.PaymentFailed);
    }

    [Fact]
    public async Task Handle_WhenPaymentFailed_RestoresProductStock()
    {
        var product = ProductFixtures.CreateProduct(quantityInStock: 5);
        var orderItem = new OrderItem
        {
            ItemOrdered = new ProductOrderItem { ProductId = product.Id, Name = product.Name },
            Price = 1500,
            Quantity = 2
        };
        var order = OrderFixtures.CreateOrder(items: [orderItem]);

        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.payment_failed",
            Status: "failed",
            IntentId: order.PaymentIntentId,
            Amount: 0);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        product.QuantityInStock.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WhenPaymentFailed_PublishesStockChangedEvent()
    {
        var product = ProductFixtures.CreateProduct(quantityInStock: 5);
        var orderItem = new OrderItem
        {
            ItemOrdered = new ProductOrderItem { ProductId = product.Id, Name = product.Name },
            Price = 1500,
            Quantity = 2
        };
        var order = OrderFixtures.CreateOrder(items: [orderItem]);

        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.payment_failed",
            Status: "failed",
            IntentId: order.PaymentIntentId,
            Amount: 0);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _eventPublisher.Verify(
            e => e.PublishEventAsync(
                It.Is<ProductStockChangedIntegrationEvent>(ev =>
                    ev.ProductId == product.Id && ev.NewQuantity == 7),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_ReturnsFailure()
    {
        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Stripe error"));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error handling Stripe webhook");
    }

    [Fact]
    public async Task Handle_WhenPaymentFailed_AndOrderNotFound_ThrowsException()
    {
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.payment_failed",
            Status: "failed",
            IntentId: "unknown_intent",
            Amount: 0);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_AndOrderNotFound_ReturnsSuccess()
    {
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: "unknown_intent",
            Amount: 0);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenPaymentFailed_AndProductNotFound_ReturnsSuccess()
    {
        var orderItem = new OrderItem
        {
            ItemOrdered = new ProductOrderItem { ProductId = "missing-product", Name = "Missing" },
            Price = 1000,
            Quantity = 1
        };
        var order = OrderFixtures.CreateOrder(items: [orderItem]);

        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.payment_failed",
            Status: "failed",
            IntentId: order.PaymentIntentId,
            Amount: 0);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepo
            .Setup(r => r.GetByIdAsync("missing-product", It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::Domain.ProductAggregate.CatalogProduct?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── NEW: 5.3 email wiring tests ───────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_WithBillingAddress_SendsEmailWithBillingName()
    {
        var order = OrderFixtures.CreateOrder(buyerEmail: "john@example.com");
        order.BillingAddress.Name = "John Buyer";

        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: order.GetTotal());

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(
                It.Is<PaymentConfirmedEmailRequest>(r =>
                    r.ToEmail == "john@example.com" &&
                    r.ToName == "John Buyer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_WithNullBillingAddress_FallsBackToBuyerEmail()
    {
        var order = OrderFixtures.CreateOrder(buyerEmail: "fallback@example.com");
        order.BillingAddress = null!;

        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: order.GetTotal());

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(
                It.Is<PaymentConfirmedEmailRequest>(r =>
                    r.ToName == "fallback@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_AndOrderNotFound_DoesNotSendEmail()
    {
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: "unknown_intent",
            Amount: 0);

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(It.IsAny<PaymentConfirmedEmailRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandlePaymentSucceeded_PublishesOrderBillingRequestedIntegrationEvent_WhenPaymentRecieved()
    {
        var order = OrderFixtures.CreateOrder(subtotal: 5000, deliveryFee: 500);
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: order.GetTotal());

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _eventPublisher.Verify(
            e => e.PublishEventAsync(
                It.IsAny<OrderBillingRequestedIntegrationEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandlePaymentSucceeded_DoesNotPublishOrderBillingRequestedIntegrationEvent_WhenPaymentMismatch()
    {
        var order = OrderFixtures.CreateOrder(subtotal: 5000, deliveryFee: 500);
        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: 9999); // mismatch

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _eventPublisher.Verify(
            e => e.PublishEventAsync(
                It.IsAny<OrderBillingRequestedIntegrationEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeded_AndEmailFails_DoesNotPropagateException()
    {
        var order = OrderFixtures.CreateOrder(buyerEmail: "buyer@example.com");

        var stripeEvent = new StripeEventResult(
            Type: "payment_intent.succeeded",
            Status: "succeeded",
            IntentId: order.PaymentIntentId,
            Amount: order.GetTotal());

        _paymentService
            .Setup(p => p.ConstructStripeEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(stripeEvent);

        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBasket?)null);

        _emailService
            .Setup(e => e.SendAsync(It.IsAny<PaymentConfirmedEmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Email failure"));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
