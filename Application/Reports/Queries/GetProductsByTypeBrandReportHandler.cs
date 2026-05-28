using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetProductsByTypeBrandReportHandler(IProductReadRepository productReadRepository)
    : IRequestHandler<GetProductsByTypeBrandReportQuery, Result<List<ProductsByTypeBrandReportDto>>>
{
    public async Task<Result<List<ProductsByTypeBrandReportDto>>> Handle(
        GetProductsByTypeBrandReportQuery request,
        CancellationToken cancellationToken)
    {
        // ProductReadRepository.GetQueryable() es SQL-traducible.
        // GroupBy sobre dos campos escalares (Type, Brand) se traduce a GROUP BY en PostgreSQL.
        // El tipo anonimo intermedio garantiza traduccion antes de proyectar al DTO final.
        var agrupados = await productReadRepository.GetQueryable()
            .GroupBy(p => new { p.Type, p.Brand })
            .Select(g => new
            {
                Type  = g.Key.Type,
                Brand = g.Key.Brand,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var dto = agrupados.Select(a => new ProductsByTypeBrandReportDto
        {
            Type  = a.Type,
            Brand = a.Brand,
            Count = a.Count
        }).ToList();

        return Result<List<ProductsByTypeBrandReportDto>>.Success(dto);
    }
}
