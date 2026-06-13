using Application.Core.Validations;
using Application.Quotation.DTOs;
using MediatR;

namespace Application.Quotation.Commands;

public class UpdateQuotationRuleCommand : IRequest<Result<QuotationRuleDto>>
{
    public required string Id { get; set; }
    public required UpdateQuotationRuleDto Dto { get; set; }
}
