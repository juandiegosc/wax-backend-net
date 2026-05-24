using Application.Core;
using Application.Core.Validations;
using MediatR;

namespace Application.Basket.Commands;

public class RemoveBasketItemCommand : IRequest<Result<bool>>
{
    public required string ProductId { get; set; }
    public required int Quantity { get; set; }
    public required string BasketId { get; set; }
}
