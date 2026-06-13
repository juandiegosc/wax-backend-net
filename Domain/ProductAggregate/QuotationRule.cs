using Domain.Entities;

namespace Domain.ProductAggregate;

public class QuotationRule : BaseEntity
{
    public required string Key { get; init; }
    public decimal Value { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; private set; } = false;

    public void UpdateValue(decimal value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "El valor de la regla debe ser mayor a cero.");
        Value = value;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void MarkAsDefault()
    {
        IsDefault = true;
    }
}
