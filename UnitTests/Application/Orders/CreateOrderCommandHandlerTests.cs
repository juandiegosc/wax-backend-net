using Application.IntegrationEvents.OrderEvents;
using Application.IntegrationEvents.ProductEvents;
using Application.Interfaces.Services;
using Application.Interfaces.Publish;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Orders.Commands;
using Application.Orders.DTOs;
using Domain.Entities;
using Domain.Enumerators;
using Domain.OrderAggregate;
using Domain.ProductAggregate;
using DomainUser = Domain.Entities.User;
using UnitTests.Helpers.Fixtures;

namespace UnitTests.Application.Orders;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserAccessor> _userAccessor = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _handler = new CreateOrderCommandHandler(
            _basketRepo.Object,
            _orderRepo.Object,
            _unitOfWork.Object,
            _userAccessor.Object,
            _eventPublisher.Object);

        _eventPublisher.SetReturnsDefault(Task.CompletedTask);
    }

    private static CreateOrderDto BuildOrderDto() => new()
    {
        PaymentSummary = OrderFixtures.CreatePaymentSummary()
    };

    private static DomainUser BuildRegisteredUser() => new()
    {
        Email = "buyer@test.com",
        UserName = "buyer",
        BillingAddress = OrderFixtures.CreateBillingAddress(),
        BillingAddressId = "addr-1"
    };

    private void SetupRegisteredUser(DomainUser? user = null)
    {
        _userAccessor
            .Setup(u => u.GetUserWithBillingAddressAsync())
            .ReturnsAsync(user ?? BuildRegisteredUser());

        _userAccessor
            .Setup(u => u.GetUserRolesAsync())
            .ReturnsAsync([Roles.Registered]);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_user_missing");
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _userAccessor
            .Setup(u => u.GetUserWithBillingAddressAsync())
            .ReturnsAsync((DomainUser?)null);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        _userAccessor.Verify(u => u.GetUserRolesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotRegistered_ReturnsFailure()
    {
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_not_registered");
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _userAccessor
            .Setup(u => u.GetUserWithBillingAddressAsync())
            .ReturnsAsync(BuildRegisteredUser());
        _userAccessor
            .Setup(u => u.GetUserRolesAsync())
            .ReturnsAsync([Roles.Enrolled]);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Only registered users can place orders");
    }

    [Fact]
    public async Task Handle_WhenBasketNotFound_ReturnsFailure()
    {
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::Domain.Entities.Basket?)null);
        SetupRegisteredUser();

        var command = new CreateOrderCommand { BasketId = "b1", OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Basket not found or is empty");
    }

    [Fact]
    public async Task Handle_WhenBasketHasNoPaymentIntent_ReturnsFailure()
    {
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: null);
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        SetupRegisteredUser();

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Basket not found or is empty");
    }

    [Fact]
    public async Task Handle_WhenItemOutOfStock_ReturnsFailure()
    {
        var product = ProductFixtures.CreateProduct(quantityInStock: 0);
        var item = BasketFixtures.CreateBasketItem(product.Id, quantity: 5);
        ((CatalogProduct)item.Product).QuantityInStock = 0;
        var basket = BasketFixtures.CreateBasketWithItems(
            paymentIntentId: "pi_test",
            items: [item]);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        SetupRegisteredUser();

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("One or more items in the basket are out of stock");
    }

    [Fact]
    public async Task Handle_WhenOrderNotExisting_CreatesNewOrderAndPublishesEvents()
    {
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_new");
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        SetupRegisteredUser();
        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync("pi_new", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Order? capturedOrder = null;
        _orderRepo
            .Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(order => capturedOrder = order);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orderRepo.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        capturedOrder.Should().NotBeNull();
        capturedOrder!.BillingAddressId.Should().Be("addr-1");
        capturedOrder!.OrderStatus.Should().Be(OrderStatus.Pending);
        _eventPublisher.Verify(e => e.PublishEventAsync(
            It.IsAny<OrderCreatedIntegrationEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(e => e.PublishEventAsync(
            It.IsAny<ProductStockChangedIntegrationEvent>(),
            It.IsAny<CancellationToken>()), Times.Exactly(basket.Items.Count));
    }

    [Fact]
    public async Task Handle_WhenBasketHasCustomProduct_SetsOrderStatusToCustomOrder()
    {
        var customProduct = CustomProductFixtures.CreateCustomProduct(status: CustomProductStatus.AddedToBasket);
        var item = BasketFixtures.CreateBasketItem(quantity: 1, product: customProduct);
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_custom", items: [item]);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        SetupRegisteredUser();
        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync("pi_custom", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Order? capturedOrder = null;
        _orderRepo
            .Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(order => capturedOrder = order);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.OrderStatus.Should().Be(OrderStatus.CustomOrder);
        _eventPublisher.Verify(e => e.PublishEventAsync(
            It.IsAny<ProductStockChangedIntegrationEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderAlreadyExists_UpdatesExistingOrder()
    {
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_existing");
        var existingOrder = OrderFixtures.CreateOrder(paymentIntentId: "pi_existing");

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        SetupRegisteredUser();
        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync("pi_existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orderRepo.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        existingOrder.OrderItems.Should().HaveCount(basket.Items.Count);
    }

    [Fact]
    public async Task Handle_SubtotalOver10000_SetsDeliveryFeeToZero()
    {
        var product = ProductFixtures.CreateProduct(price: 15000, quantityInStock: 10);
        var item = BasketFixtures.CreateBasketItem(quantity: 1, product: product);
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_free", items: [item]);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        SetupRegisteredUser();
        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync("pi_free", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Order? capturedOrder = null;
        _orderRepo.Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        await _handler.Handle(command, CancellationToken.None);

        capturedOrder!.DeliveryFee.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenSaveFails_ReturnsFailure()
    {
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_save_fail");
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        SetupRegisteredUser();
        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync("pi_save_fail", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Failed to create order");
    }

    [Fact]
    public async Task Handle_PublishesOrderCreatedEvent_WithUserIdPopulated()
    {
        var user = new DomainUser
        {
            Id = "user-id-xyz",
            Email = "buyer@test.com",
            UserName = "buyer",
            BillingAddress = OrderFixtures.CreateBillingAddress(),
            BillingAddressId = "addr-1"
        };
        var basket = BasketFixtures.CreateBasketWithItems(paymentIntentId: "pi_userid");

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        _userAccessor
            .Setup(u => u.GetUserWithBillingAddressAsync())
            .ReturnsAsync(user);
        _userAccessor
            .Setup(u => u.GetUserRolesAsync())
            .ReturnsAsync([Roles.Registered]);
        _orderRepo
            .Setup(r => r.GetByPaymentIntentIdAsync("pi_userid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        OrderCreatedIntegrationEvent? capturedEvent = null;
        _eventPublisher
            .Setup(e => e.PublishEventAsync(It.IsAny<OrderCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<OrderCreatedIntegrationEvent, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand { BasketId = basket.BasketId, OrderDto = BuildOrderDto() };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be("user-id-xyz");
    }
}
