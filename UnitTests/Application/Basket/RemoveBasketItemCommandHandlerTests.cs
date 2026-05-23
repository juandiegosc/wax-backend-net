using Application.Basket.Commands;
using Application.Interfaces.Repositories.WriteRepositories;
using UnitTests.Helpers.Fixtures;

namespace UnitTests.Application.Basket;

public class RemoveBasketItemCommandHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RemoveBasketItemCommandHandler _handler;

    public RemoveBasketItemCommandHandlerTests()
    {
        _handler = new RemoveBasketItemCommandHandler(
            _basketRepo.Object,
            _productRepo.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WhenBasketNotFound_ReturnsFailure()
    {
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::Domain.Entities.Basket?)null);

        var command = new RemoveBasketItemCommand { BasketId = "b1", ProductId = "p1", Quantity = 1 };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Basket not found");
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsFailure()
    {
        var basket = BasketFixtures.CreateBasket();
        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::Domain.ProductAggregate.CatalogProduct?)null);

        var command = new RemoveBasketItemCommand { BasketId = basket.BasketId, ProductId = "p1", Quantity = 1 };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkFails_ReturnsFailure()
    {
        var product = ProductFixtures.CreateProduct();
        var basket = BasketFixtures.CreateBasketWithItems(items: [BasketFixtures.CreateBasketItem(product: product)]);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        _productRepo
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new RemoveBasketItemCommand { BasketId = basket.BasketId, ProductId = product.Id, Quantity = 1 };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Failed to remove item from basket");
    }

    [Fact]
    public async Task Handle_WhenValid_RemovesItemAndReturnsSuccess()
    {
        var product = ProductFixtures.CreateProduct();
        var basket = BasketFixtures.CreateBasketWithItems(items: [BasketFixtures.CreateBasketItem(product: product, quantity: 3)]);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        _productRepo
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RemoveBasketItemCommand { BasketId = basket.BasketId, ProductId = product.Id, Quantity = 1 };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenLastItemRemoved_DeletesBasket()
    {
        var product = ProductFixtures.CreateProduct();
        var basket = BasketFixtures.CreateBasketWithItems(items: [BasketFixtures.CreateBasketItem(product: product, quantity: 1)]);

        _basketRepo
            .Setup(r => r.GetBasketWithItemsAsync(basket.BasketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);
        _productRepo
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork
            .Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RemoveBasketItemCommand { BasketId = basket.BasketId, ProductId = product.Id, Quantity = 1 };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _basketRepo.Verify(r => r.Remove(basket), Times.Once);
    }
}
