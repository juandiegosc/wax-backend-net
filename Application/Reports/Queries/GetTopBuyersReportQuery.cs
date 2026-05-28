using Application.Core.Validations;
using Application.Reports.DTOs;
using MediatR;

namespace Application.Reports.Queries;

public class GetTopBuyersReportQuery : IRequest<Result<List<TopBuyerReportDto>>>
{
    public int Limit { get; set; } = 10;
}
