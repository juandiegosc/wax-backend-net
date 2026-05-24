using Application.Core;
using Application.Core.Validations;
using Application.Interfaces.Repositories.WriteRepositories;
using MediatR;

namespace Application.Basket.Commands;

public class RemoveBasketItemCommandHandler(
    IBasketRepository basketRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveBasketItemCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RemoveBasketItemCommand request, CancellationToken cancellationToken)
    {
        var basket = await basketRepository.GetBasketWithItemsAsync(request.BasketId, cancellationToken);

        if (basket == null) return Result<bool>.Failure("Basket not found");

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null) return Result<bool>.Failure("Product not found");

        basket.RemoveItem(product.Id, request.Quantity);

        bool basketDeleted = basket.Items.Count == 0;
        if (basketDeleted)
            basketRepository.Remove(basket);

        var result = await unitOfWork.CompleteAsync(cancellationToken);
        if (!result) return Result<bool>.Failure("Failed to remove item from basket");

        return Result<bool>.Success(basketDeleted);
    }
}
