using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetTopBuyersReportHandler(IOrderReadRepository orderReadRepository)
    : IRequestHandler<GetTopBuyersReportQuery, Result<List<TopBuyerReportDto>>>
{
    public async Task<Result<List<TopBuyerReportDto>>> Handle(
        GetTopBuyersReportQuery request,
        CancellationToken cancellationToken)
    {
        var rows = orderReadRepository.GetOrderReportRows();

        // Proyeccion con tipo anonimo primero para garantizar traduccion SQL en EF Core + Npgsql.
        // El mapeo a TopBuyerReportDto se hace en memoria sobre el resultado ya reducido (Take).
        var agrupados = await rows
            .GroupBy(r => r.BuyerEmail)
            .Select(g => new
            {
                BuyerEmail = g.Key,
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(r => r.Total)
            })
            .OrderByDescending(b => b.TotalRevenue)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var topBuyers = agrupados.Select(a => new TopBuyerReportDto
        {
            BuyerEmail = a.BuyerEmail,
            UserId = null, // UserId no se incluye para evitar sub-selects no traducibles
            OrderCount = a.OrderCount,
            TotalRevenue = a.TotalRevenue
        }).ToList();

        return Result<List<TopBuyerReportDto>>.Success(topBuyers);
    }
}
