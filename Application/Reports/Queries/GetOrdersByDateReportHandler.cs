using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetOrdersByDateReportHandler(IOrderReadRepository orderReadRepository)
    : IRequestHandler<GetOrdersByDateReportQuery, Result<List<OrdersByDateReportDto>>>
{
    public async Task<Result<List<OrdersByDateReportDto>>> Handle(
        GetOrdersByDateReportQuery request,
        CancellationToken cancellationToken)
    {
        var rows = orderReadRepository.GetOrderReportRows(request.From, request.To);

        // Agrupacion por dia. GroupBy sobre { Year, Month, Day } es siempre SQL-traducible
        // en EF Core + Npgsql (fallback seguro si .Date no traduce en alguna config).
        var porDia = await rows
            .GroupBy(r => new
            {
                r.CreatedAt.Year,
                r.CreatedAt.Month,
                r.CreatedAt.Day
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                OrderCount = g.Count(),
                Revenue = g.Sum(r => r.Total)
            })
            .OrderBy(g => g.Year)
            .ThenBy(g => g.Month)
            .ThenBy(g => g.Day)
            .ToListAsync(cancellationToken);

        var resultado = porDia.Select(g => new OrdersByDateReportDto
        {
            Date = new DateTime(g.Year, g.Month, g.Day, 0, 0, 0, DateTimeKind.Utc),
            OrderCount = g.OrderCount,
            Revenue = g.Revenue
        }).ToList();

        return Result<List<OrdersByDateReportDto>>.Success(resultado);
    }
}
