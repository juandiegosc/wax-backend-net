using Application.Core.Validations;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Quotation.DTOs;
using Application.Quotation.Extensions;
using MediatR;

namespace Application.Quotation.Commands;

public class UpdateQuotationRuleCommandHandler(
    IQuotationRuleRepository repository,
    IUnitOfWork unitOfWork,
    IQuotationRulesCache cache)
    : IRequestHandler<UpdateQuotationRuleCommand, Result<QuotationRuleDto>>
{
    public async Task<Result<QuotationRuleDto>> Handle(UpdateQuotationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (rule == null)
            return Result<QuotationRuleDto>.Failure("Regla de cotizacion no encontrada.", 404);

        request.Dto.ApplyTo(rule);
        await unitOfWork.CompleteAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);

        return Result<QuotationRuleDto>.Success(rule.ToDto());
    }
}
