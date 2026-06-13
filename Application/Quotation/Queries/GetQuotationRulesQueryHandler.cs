using Application.Core.Validations;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Quotation.DTOs;
using Application.Quotation.Extensions;
using MediatR;

namespace Application.Quotation.Queries;

public class GetQuotationRulesQueryHandler(IQuotationRuleRepository repository)
    : IRequestHandler<GetQuotationRulesQuery, Result<IReadOnlyList<QuotationRuleDto>>>
{
    public async Task<Result<IReadOnlyList<QuotationRuleDto>>> Handle(GetQuotationRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await repository.ListAsync(request.ActiveOnly, cancellationToken);
        var dtos = rules.Select(r => r.ToDto()).ToList();
        return Result<IReadOnlyList<QuotationRuleDto>>.Success(dtos);
    }
}
