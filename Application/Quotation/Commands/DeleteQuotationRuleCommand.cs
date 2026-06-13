using Application.Core.Validations;
using MediatR;

namespace Application.Quotation.Commands;

public class DeleteQuotationRuleCommand : IRequest<Result<bool>>
{
    public required string Id { get; set; }
}
