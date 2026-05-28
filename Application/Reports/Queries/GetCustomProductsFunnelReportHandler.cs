using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Domain.ProductAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetCustomProductsFunnelReportHandler(ICustomProductReadRepository customProductReadRepository)
    : IRequestHandler<GetCustomProductsFunnelReportQuery, Result<List<CustomProductsFunnelReportDto>>>
{
    // Todos los valores del enum CustomProductStatus, en el orden de la cadena de negocio.
    private static readonly string[] TodosLosEstados =
    [
        nameof(CustomProductStatus.PendingQuotation),
        nameof(CustomProductStatus.AwaitingAdminReview),
        nameof(CustomProductStatus.AwaitingCustomerReview),
        nameof(CustomProductStatus.Approved),
        nameof(CustomProductStatus.Rejected),
        nameof(CustomProductStatus.AddedToBasket),
    ];

    public async Task<Result<List<CustomProductsFunnelReportDto>>> Handle(
        GetCustomProductsFunnelReportQuery request,
        CancellationToken cancellationToken)
    {
        // GroupBy(Status) -> Count se ejecuta EN BD (proyeccion escalar GetCustomProductReportRows).
        // Solo retorna filas para los estados que tienen al menos un registro.
        var conteoPorEstado = await customProductReadRepository.GetCustomProductReportRows()
            .GroupBy(r => r.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count  = g.Count()
            })
            .ToListAsync(cancellationToken);

        // Relleno en memoria: garantiza que TODOS los valores del enum aparezcan,
        // incluyendo los que tienen 0 registros (requisito RF-12 / RFC-12-A).
        var conteoDict = conteoPorEstado.ToDictionary(e => e.Status, e => e.Count);

        var dto = TodosLosEstados.Select(estado => new CustomProductsFunnelReportDto
        {
            Status = estado,
            Count  = conteoDict.GetValueOrDefault(estado, 0)
        }).ToList();

        return Result<List<CustomProductsFunnelReportDto>>.Success(dto);
    }
}
