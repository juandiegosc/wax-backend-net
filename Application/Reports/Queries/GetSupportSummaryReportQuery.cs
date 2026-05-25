using Application.Core.Validations;
using Application.Reports.DTOs;
using MediatR;

namespace Application.Reports.Queries;

public class GetSupportSummaryReportQuery : IRequest<Result<SupportSummaryReportDto>>;
