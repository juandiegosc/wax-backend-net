using Application.Quotation.Commands;
using FluentValidation;

namespace Application.Quotation.Validators;

public class CreateQuotationRuleCommandValidator : AbstractValidator<CreateQuotationRuleCommand>
{
    public CreateQuotationRuleCommandValidator()
    {
        RuleFor(x => x.Dto.Key)
            .NotEmpty().WithMessage("La clave es obligatoria.")
            .MaximumLength(100).WithMessage("La clave no puede superar 100 caracteres.")
            .Must(k => k == k.Trim()).WithMessage("La clave no puede tener espacios al inicio o al final.");

        RuleFor(x => x.Dto.Value)
            .GreaterThan(0).WithMessage("El valor debe ser mayor a cero.");

        RuleFor(x => x.Dto.Description)
            .MaximumLength(500).WithMessage("La descripcion no puede superar 500 caracteres.")
            .When(x => x.Dto.Description != null);
    }
}
