using Application.Core.Validations;
using Application.Quotation.DTOs;
using MediatR;

namespace Application.Quotation.Queries;

public class GetQuotationRuleByIdQuery : IRequest<Result<QuotationRuleDto>>
{
    public required string Id { get; set; }
}
