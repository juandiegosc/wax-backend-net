namespace Application.Quotation.DTOs;

public class CreateQuotationRuleDto
{
    public required string Key { get; set; }
    public decimal Value { get; set; }
    public string? Description { get; set; }
}
