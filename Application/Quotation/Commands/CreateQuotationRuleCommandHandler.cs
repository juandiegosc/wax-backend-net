using Application.Core.Validations;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Quotation.DTOs;
using Application.Quotation.Extensions;
using MediatR;

namespace Application.Quotation.Commands;

public class CreateQuotationRuleCommandHandler(
    IQuotationRuleRepository repository,
    IUnitOfWork unitOfWork,
    IQuotationRulesCache cache)
    : IRequestHandler<CreateQuotationRuleCommand, Result<QuotationRuleDto>>
{
    public async Task<Result<QuotationRuleDto>> Handle(CreateQuotationRuleCommand request, CancellationToken cancellationToken)
    {
        var exists = await repository.ExistsByKeyAsync(request.Dto.Key, cancellationToken);
        if (exists)
            return Result<QuotationRuleDto>.Failure($"Ya existe una regla con la clave '{request.Dto.Key}'.", 400);

        var rule = request.Dto.ToEntity();
        await repository.AddAsync(rule, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);

        return Result<QuotationRuleDto>.Success(rule.ToDto());
    }
}
