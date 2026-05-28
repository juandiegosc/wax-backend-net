using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetProductsStockReportHandler(IProductReadRepository productReadRepository)
    : IRequestHandler<GetProductsStockReportQuery, Result<ProductsStockReportDto>>
{
    public async Task<Result<ProductsStockReportDto>> Handle(
        GetProductsStockReportQuery request,
        CancellationToken cancellationToken)
    {
        // ProductReadRepository.GetQueryable() proyecta solo campos escalares sobre ProductReadModel.
        // EF Core traduce GroupBy/Count/Where a SQL en PostgreSQL (NoTracking).
        var productos = productReadRepository.GetQueryable();

        // Tres consultas independientes, cada una traducida a SQL en BD (CountAsync con WHERE).
        // Se ejecutan en SERIE: EF Core no permite operaciones concurrentes sobre la misma
        // instancia de DbContext (Task.WhenAll lanzaria "second operation started on this context").
        var total   = await productos.CountAsync(cancellationToken);
        var agotado = await productos.CountAsync(p => p.QuantityInStock == 0, cancellationToken);
        var bajo    = await productos.CountAsync(
            p => p.QuantityInStock > 0 && p.QuantityInStock <= request.Threshold, cancellationToken);
        var normal  = total - agotado - bajo;

        var dto = new ProductsStockReportDto
        {
            TotalProducts = total,
            OutOfStock    = agotado,
            LowStock      = bajo,
            InStock       = normal
        };

        return Result<ProductsStockReportDto>.Success(dto);
    }
}
