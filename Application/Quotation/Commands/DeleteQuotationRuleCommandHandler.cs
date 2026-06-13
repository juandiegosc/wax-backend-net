using Application.Core.Validations;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Quotation.Commands;

public class DeleteQuotationRuleCommandHandler(
    IQuotationRuleRepository repository,
    IUnitOfWork unitOfWork,
    IQuotationRulesCache cache)
    : IRequestHandler<DeleteQuotationRuleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteQuotationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (rule == null)
            return Result<bool>.Failure("Regla de cotizacion no encontrada.", 404);

        if (rule.IsDefault)
            return Result<bool>.Failure("No se puede eliminar una regla predeterminada del sistema.", 400);

        rule.Deactivate();
        await unitOfWork.CompleteAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
