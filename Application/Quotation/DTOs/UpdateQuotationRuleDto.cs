namespace Application.Quotation.DTOs;

public class UpdateQuotationRuleDto
{
    public decimal Value { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
