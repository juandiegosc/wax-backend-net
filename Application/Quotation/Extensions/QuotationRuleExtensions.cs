using Application.Quotation.DTOs;
using Domain.ProductAggregate;

namespace Application.Quotation.Extensions;

public static class QuotationRuleExtensions
{
    public static QuotationRule ToEntity(this CreateQuotationRuleDto dto)
    {
        return new QuotationRule
        {
            Key = dto.Key,
            Value = dto.Value,
            Description = dto.Description,
            IsActive = true
        };
    }

    public static void ApplyTo(this UpdateQuotationRuleDto dto, QuotationRule rule)
    {
        rule.UpdateValue(dto.Value);
        rule.UpdateDescription(dto.Description);

        if (dto.IsActive)
            rule.Activate();
        else
            rule.Deactivate();
    }

    public static QuotationRuleDto ToDto(this QuotationRule rule)
    {
        return new QuotationRuleDto(
            rule.Id,
            rule.Key,
            rule.Value,
            rule.Description,
            rule.IsActive,
            rule.IsDefault);
    }
}
