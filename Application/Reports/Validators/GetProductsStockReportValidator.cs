using Application.Reports.Queries;
using FluentValidation;

namespace Application.Reports.Validators;

public class GetProductsStockReportValidator : AbstractValidator<GetProductsStockReportQuery>
{
    public GetProductsStockReportValidator()
    {
        RuleFor(x => x.Threshold)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El umbral de stock no puede ser negativo.");
    }
}
