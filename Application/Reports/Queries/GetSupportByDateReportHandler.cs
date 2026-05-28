using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetSupportByDateReportHandler(ISupportTicketReadRepository supportTicketReadRepository)
    : IRequestHandler<GetSupportByDateReportQuery, Result<List<SupportByDateReportDto>>>
{
    public async Task<Result<List<SupportByDateReportDto>>> Handle(
        GetSupportByDateReportQuery request,
        CancellationToken cancellationToken)
    {
        // Agrupacion por dia sobre { Year, Month, Day } — siempre SQL-traducible en Npgsql.
        var porDia = await supportTicketReadRepository.GetSupportTickets()
            .Where(t => t.CreatedAt >= request.From && t.CreatedAt <= request.To)
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                TicketCount = g.Count()
            })
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Month)
            .ThenBy(g => g.Day)
            .ToListAsync(cancellationToken);

        var resultado = porDia.Select(g => new SupportByDateReportDto
        {
            Date = new DateTime(g.Year, g.Month, g.Day, 0, 0, 0, DateTimeKind.Utc),
            TicketCount = g.TicketCount
        }).ToList();

        return Result<List<SupportByDateReportDto>>.Success(resultado);
    }
}
