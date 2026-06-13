using Application.Quotation.Commands;
using FluentValidation;

namespace Application.Quotation.Validators;

public class UpdateQuotationRuleCommandValidator : AbstractValidator<UpdateQuotationRuleCommand>
{
    public UpdateQuotationRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador es obligatorio.");

        RuleFor(x => x.Dto.Value)
            .GreaterThan(0).WithMessage("El valor debe ser mayor a cero.");

        RuleFor(x => x.Dto.Description)
            .MaximumLength(500).WithMessage("La descripcion no puede superar 500 caracteres.")
            .When(x => x.Dto.Description != null);
    }
}
