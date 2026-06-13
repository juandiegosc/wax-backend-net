using Application.Core.Validations;
using Application.Quotation.DTOs;
using MediatR;

namespace Application.Quotation.Commands;

public class CreateQuotationRuleCommand : IRequest<Result<QuotationRuleDto>>
{
    public required CreateQuotationRuleDto Dto { get; set; }
}
