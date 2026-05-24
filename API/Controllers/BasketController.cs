using Application.Basket.Commands;
using Application.Basket.DTOs;
using Application.Basket.Interfaces;
using Application.Basket.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class BasketController(IBasketProvider basketProvider) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(BasketDto), 200)]
    public async Task<ActionResult<BasketDto>> GetBasket()
    {
        var basketId = basketProvider.GetBasketId() ?? string.Empty;
        return await HandleQuery(new GetBasketQuery { BasketId = basketId });
    }

    [HttpPost]
    [ProducesResponseType(typeof(BasketDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<BasketDto>> AddItemToBasket(string productId, int quantity)
    {
        var basketId = basketProvider.GetBasketId();
        var result = await HandleCommandWithResult(new AddItemCommand
            { 
                ProductId = productId, 
                Quantity = quantity, 
                BasketId = basketId ?? string.Empty
            });

        if (result.IsSuccess && string.IsNullOrWhiteSpace(basketId))
        {
            basketProvider.SetBasketId(result.Value!.BasketId);
        }
        
        return HandleResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Unit>> RemoveItemFromBasket(string productId, int quantity)
    {
        var basketId = basketProvider.GetBasketId() ?? string.Empty;
        var result = await HandleCommandWithResult(new RemoveBasketItemCommand { ProductId = productId, Quantity = quantity, BasketId = basketId });

        if (!result.IsSuccess) return BadRequest(result.Error);

        if (result.Value) basketProvider.DeleteBasketId();

        return Ok();
    }
}
