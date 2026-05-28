using Application.Reports.Queries;
using FluentValidation;

namespace Application.Reports.Validators;

public class GetTopBuyersReportValidator : AbstractValidator<GetTopBuyersReportQuery>
{
    public GetTopBuyersReportValidator()
    {
        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("El limite debe ser mayor que 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("El limite no puede superar 100.");
    }
}
