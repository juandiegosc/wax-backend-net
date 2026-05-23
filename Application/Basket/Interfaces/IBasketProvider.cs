namespace Application.Basket.Interfaces;

public interface IBasketProvider
{
    string? GetBasketId();
    void SetBasketId(string basketId);
    void DeleteBasketId();
}