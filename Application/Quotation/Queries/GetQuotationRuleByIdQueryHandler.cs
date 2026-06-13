using Application.Core.Validations;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Quotation.DTOs;
using Application.Quotation.Extensions;
using MediatR;

namespace Application.Quotation.Queries;

public class GetQuotationRuleByIdQueryHandler(IQuotationRuleRepository repository)
    : IRequestHandler<GetQuotationRuleByIdQuery, Result<QuotationRuleDto>>
{
    public async Task<Result<QuotationRuleDto>> Handle(GetQuotationRuleByIdQuery request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule == null)
            return Result<QuotationRuleDto>.Failure("Regla de cotizacion no encontrada.", 404);

        return Result<QuotationRuleDto>.Success(rule.ToDto());
    }
}
