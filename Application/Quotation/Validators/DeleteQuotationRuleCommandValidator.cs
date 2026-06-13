using Application.Quotation.Commands;
using FluentValidation;

namespace Application.Quotation.Validators;

public class DeleteQuotationRuleCommandValidator : AbstractValidator<DeleteQuotationRuleCommand>
{
    public DeleteQuotationRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
