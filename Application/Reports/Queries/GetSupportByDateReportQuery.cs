using Application.Core.Validations;
using Application.Reports.DTOs;
using MediatR;

namespace Application.Reports.Queries;

public class GetSupportByDateReportQuery : IRequest<Result<List<SupportByDateReportDto>>>
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
