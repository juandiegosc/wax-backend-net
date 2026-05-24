using API.Controllers;
using Application.Basket.Commands;
using Application.Basket.DTOs;
using Application.Basket.Interfaces;
using Application.Basket.Queries;
using Application.Core.Validations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UnitTests.Helpers;

namespace UnitTests.API.Controllers;

public class BasketControllerTests
{
    private readonly Mock<IBasketProvider> _basketProvider = new();
    private readonly Mock<IMediator> _mediator;
    private readonly BasketController _controller;

    public BasketControllerTests()
    {
        (_mediator, _controller) = ControllerTestFactory.Create(new BasketController(_basketProvider.Object));
    }

    [Fact]
    public async Task GetBasket_UsesBasketProviderIdAndDelegates()
    {
        const string basketId = "basket-123";
        _basketProvider.Setup(p => p.GetBasketId()).Returns(basketId);

        GetBasketQuery? capturedQuery = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<GetBasketQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<BasketDto>>, CancellationToken>((q, _) => capturedQuery = (GetBasketQuery)q)
            .ReturnsAsync(Result<BasketDto>.Success(new BasketDto { BasketId = basketId }));

        await _controller.GetBasket();

        capturedQuery.Should().NotBeNull();
        capturedQuery!.BasketId.Should().Be(basketId);
    }

    [Fact]
    public async Task GetBasket_WithNullBasketId_UsesEmptyString()
    {
        _basketProvider.Setup(p => p.GetBasketId()).Returns((string?)null);

        GetBasketQuery? capturedQuery = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<GetBasketQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<BasketDto>>, CancellationToken>((q, _) => capturedQuery = (GetBasketQuery)q)
            .ReturnsAsync(Result<BasketDto>.Success(new BasketDto { BasketId = string.Empty }));

        await _controller.GetBasket();

        capturedQuery!.BasketId.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItemToBasket_NewBasket_SetsBasketIdOnSuccess()
    {
        const string newBasketId = "new-basket-id";
        _basketProvider.Setup(p => p.GetBasketId()).Returns((string?)null);

        _mediator
            .Setup(m => m.Send(It.IsAny<AddItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BasketDto>.Success(new BasketDto { BasketId = newBasketId }));

        await _controller.AddItemToBasket("product-1", 1);

        _basketProvider.Verify(p => p.SetBasketId(newBasketId), Times.Once);
    }

    [Fact]
    public async Task AddItemToBasket_ExistingBasket_DoesNotSetBasketId()
    {
        _basketProvider.Setup(p => p.GetBasketId()).Returns("existing-basket-id");

        _mediator
            .Setup(m => m.Send(It.IsAny<AddItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BasketDto>.Success(new BasketDto { BasketId = "existing-basket-id" }));

        await _controller.AddItemToBasket("product-1", 1);

        _basketProvider.Verify(p => p.SetBasketId(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddItemToBasket_WhenFails_DoesNotSetBasketId()
    {
        _basketProvider.Setup(p => p.GetBasketId()).Returns((string?)null);

        _mediator
            .Setup(m => m.Send(It.IsAny<AddItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BasketDto>.Failure("Failed to add item to basket"));

        await _controller.AddItemToBasket("product-1", 1);

        _basketProvider.Verify(p => p.SetBasketId(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddItemToBasket_ReturnsOkResult_WhenSuccess()
    {
        var basketDto = new BasketDto { BasketId = "basket-123" };
        _basketProvider.Setup(p => p.GetBasketId()).Returns("basket-123");

        _mediator
            .Setup(m => m.Send(It.IsAny<AddItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BasketDto>.Success(basketDto));

        var result = await _controller.AddItemToBasket("product-1", 2);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(basketDto);
    }

    [Fact]
    public async Task AddItemToBasket_ReturnsBadRequest_WhenFailure()
    {
        _basketProvider.Setup(p => p.GetBasketId()).Returns("basket-123");

        _mediator
            .Setup(m => m.Send(It.IsAny<AddItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BasketDto>.Failure("Product not found"));

        var result = await _controller.AddItemToBasket("product-1", 1);

        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.Value.Should().Be("Product not found");
    }

    [Fact]
    public async Task RemoveItemFromBasket_DelegatesToCommand_WithCorrectIds()
    {
        const string basketId = "basket-123";
        const string productId = "product-abc";
        const int quantity = 2;
        _basketProvider.Setup(p => p.GetBasketId()).Returns(basketId);

        RemoveBasketItemCommand? capturedCommand = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<RemoveBasketItemCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<bool>>, CancellationToken>((c, _) => capturedCommand = (RemoveBasketItemCommand)c)
            .ReturnsAsync(Result<bool>.Success(false));

        await _controller.RemoveItemFromBasket(productId, quantity);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.BasketId.Should().Be(basketId);
        capturedCommand.ProductId.Should().Be(productId);
        capturedCommand.Quantity.Should().Be(quantity);
    }

    [Fact]
    public async Task RemoveItemFromBasket_WhenBasketDeleted_DeletesCookie()
    {
        _basketProvider.Setup(p => p.GetBasketId()).Returns("basket-123");

        _mediator
            .Setup(m => m.Send(It.IsAny<RemoveBasketItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        await _controller.RemoveItemFromBasket("product-1", 1);

        _basketProvider.Verify(p => p.DeleteBasketId(), Times.Once);
    }

    [Fact]
    public async Task RemoveItemFromBasket_WhenBasketNotDeleted_DoesNotDeleteCookie()
    {
        _basketProvider.Setup(p => p.GetBasketId()).Returns("basket-123");

        _mediator
            .Setup(m => m.Send(It.IsAny<RemoveBasketItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        await _controller.RemoveItemFromBasket("product-1", 1);

        _basketProvider.Verify(p => p.DeleteBasketId(), Times.Never);
    }
}
