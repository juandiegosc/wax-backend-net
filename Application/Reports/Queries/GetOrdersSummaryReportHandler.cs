using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Domain.OrderAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetOrdersSummaryReportHandler(IOrderReadRepository orderReadRepository)
    : IRequestHandler<GetOrdersSummaryReportQuery, Result<OrdersSummaryReportDto>>
{
    public async Task<Result<OrdersSummaryReportDto>> Handle(
        GetOrdersSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var rows = orderReadRepository.GetOrderReportRows();

        // Agregar por estado en base de datos (proyeccion escalar — SQL-traducible).
        // Solo ~7 filas de resultado; materializacion aceptable.
        var porEstado = await rows
            .GroupBy(r => r.OrderStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(r => r.Total)
            })
            .ToListAsync(cancellationToken);

        // RF-05: TotalRevenue = suma de Total sobre TODAS las ordenes (segun spec).
        // AverageTicket = TotalRevenue / TotalOrders (o 0 si no hay ordenes).
        var totalOrdenes = porEstado.Sum(e => e.Count);
        var totalRevenue = porEstado.Sum(e => e.Revenue);
        var ticketPromedio = totalOrdenes > 0
            ? totalRevenue / totalOrdenes
            : 0;

        // Construir la lista por estado incluyendo los que no tienen ordenes
        var todosLosEstados = Enum.GetNames<OrderStatus>();

        var byStatus = todosLosEstados.Select(nombre =>
        {
            var entrada = porEstado.FirstOrDefault(e => e.Status == nombre);
            var count = entrada?.Count ?? 0;
            return new OrdersByStatusReportDto
            {
                Status = nombre,
                Count = count,
                Revenue = entrada?.Revenue ?? 0,
                Percentage = totalOrdenes > 0
                    ? Math.Round((double)count / totalOrdenes * 100, 2)
                    : 0
            };
        }).ToList();

        var dto = new OrdersSummaryReportDto
        {
            TotalOrders = totalOrdenes,
            TotalRevenue = totalRevenue,
            AverageTicket = ticketPromedio,
            ByStatus = byStatus
        };

        return Result<OrdersSummaryReportDto>.Success(dto);
    }
}
