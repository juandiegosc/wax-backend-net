using Application.Core.Validations;
using Application.Quotation.DTOs;
using MediatR;

namespace Application.Quotation.Queries;

public class GetQuotationRulesQuery : IRequest<Result<IReadOnlyList<QuotationRuleDto>>>
{
    public bool? ActiveOnly { get; set; }
}
