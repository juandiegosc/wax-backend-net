using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Domain.SupportAssistAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetSupportSummaryReportHandler(ISupportTicketReadRepository supportTicketReadRepository)
    : IRequestHandler<GetSupportSummaryReportQuery, Result<SupportSummaryReportDto>>
{
    public async Task<Result<SupportSummaryReportDto>> Handle(
        GetSupportSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var tickets = supportTicketReadRepository.GetSupportTickets();

        // Dos pasadas secuenciales sobre el mismo DbContext (no concurrentes — EF Core
        // no permite operaciones simultaneas sobre un mismo contexto). Cada GroupBy
        // baja a SQL via la proyeccion Expression de GetSupportTickets().
        var porStatus = await tickets
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var porCategoria = await tickets
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // ByCategory incluye todas las categorias del enum, incluso con conteo 0.
        var byCategory = Enum.GetNames<TicketCategory>()
            .ToDictionary(
                nombre => nombre,
                nombre => porCategoria.FirstOrDefault(c => c.Category == nombre)?.Count ?? 0);

        var dto = new SupportSummaryReportDto
        {
            Total = porStatus.Sum(s => s.Count),
            Open = porStatus.FirstOrDefault(s => s.Status == nameof(TicketStatus.Open))?.Count ?? 0,
            InProgress = porStatus.FirstOrDefault(s => s.Status == nameof(TicketStatus.InProgress))?.Count ?? 0,
            Closed = porStatus.FirstOrDefault(s => s.Status == nameof(TicketStatus.Closed))?.Count ?? 0,
            ByCategory = byCategory
        };

        return Result<SupportSummaryReportDto>.Success(dto);
    }
}
