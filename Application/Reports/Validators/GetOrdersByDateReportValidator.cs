using Application.Reports.Queries;
using FluentValidation;

namespace Application.Reports.Validators;

public class GetOrdersByDateReportValidator : AbstractValidator<GetOrdersByDateReportQuery>
{
    public GetOrdersByDateReportValidator()
    {
        RuleFor(x => x.From)
            .Must((query, from) => from <= query.To)
            .WithMessage("El rango de fechas es invalido: 'from' debe ser anterior o igual a 'to'.")
            .Must((query, from) => (query.To - from).TotalDays <= 366)
            .WithMessage("El rango de fechas no puede superar 366 dias.");
    }
}
