using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Domain.OrderAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetOrdersByStatusReportHandler(IOrderReadRepository orderReadRepository)
    : IRequestHandler<GetOrdersByStatusReportQuery, Result<List<OrdersByStatusReportDto>>>
{
    public async Task<Result<List<OrdersByStatusReportDto>>> Handle(
        GetOrdersByStatusReportQuery request,
        CancellationToken cancellationToken)
    {
        var rows = orderReadRepository.GetOrderReportRows();

        var porEstado = await rows
            .GroupBy(r => r.OrderStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(r => r.Total)
            })
            .ToListAsync(cancellationToken);

        var totalOrdenes = porEstado.Sum(e => e.Count);
        var todosLosEstados = Enum.GetNames<OrderStatus>();

        var resultado = todosLosEstados.Select(nombre =>
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

        return Result<List<OrdersByStatusReportDto>>.Success(resultado);
    }
}
