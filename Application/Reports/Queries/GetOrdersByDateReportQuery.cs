using Application.Core.Validations;
using Application.Reports.DTOs;
using MediatR;

namespace Application.Reports.Queries;

public class GetOrdersByDateReportQuery : IRequest<Result<List<OrdersByDateReportDto>>>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public DateGranularity Granularity { get; set; } = DateGranularity.Day;
}
