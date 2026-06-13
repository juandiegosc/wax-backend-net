namespace Application.Quotation.DTOs;

public record QuotationRuleDto(
    string Id,
    string Key,
    decimal Value,
    string? Description,
    bool IsActive,
    bool IsDefault);
