using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Domain.ProductAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetCustomProductsPriceStatsReportHandler(ICustomProductReadRepository customProductReadRepository)
    : IRequestHandler<GetCustomProductsPriceStatsReportQuery, Result<CustomProductsPriceStatsReportDto>>
{
    public async Task<Result<CustomProductsPriceStatsReportDto>> Handle(
        GetCustomProductsPriceStatsReportQuery request,
        CancellationToken cancellationToken)
    {
        // Filtra Approved con AgreedPrice no nulo y materializa solo los precios (long).
        // La proyeccion escalar de GetCustomProductReportRows() garantiza traduccion a SQL.
        // La materializacion se limita a las filas Approved con precio: volumen controlado.
        var precios = await customProductReadRepository.GetCustomProductReportRows()
            .Where(r => r.Status == nameof(CustomProductStatus.Approved) && r.AgreedPrice != null)
            .Select(r => r.AgreedPrice!.Value)
            .ToListAsync(cancellationToken);

        // Guarda critica: si no hay filas, retornar stats null sin ejecutar Min/Max/Average
        // sobre un conjunto vacio (que lanzaria InvalidOperationException en LINQ-to-Objects
        // o retornaria NULL inesperado en SQL segun el proveedor).
        if (precios.Count == 0)
        {
            return Result<CustomProductsPriceStatsReportDto>.Success(new CustomProductsPriceStatsReportDto
            {
                ApprovedCount = 0,
                MinPrice      = null,
                MaxPrice      = null,
                AveragePrice  = null
            });
        }

        // Calculo en memoria sobre un conjunto no vacio: seguro sin division por cero.
        var dto = new CustomProductsPriceStatsReportDto
        {
            ApprovedCount = precios.Count,
            MinPrice      = precios.Min(),
            MaxPrice      = precios.Max(),
            // Average retorna double; truncamos a long (consistente con el tipo de moneda).
            AveragePrice  = (long)precios.Average()
        };

        return Result<CustomProductsPriceStatsReportDto>.Success(dto);
    }
}
