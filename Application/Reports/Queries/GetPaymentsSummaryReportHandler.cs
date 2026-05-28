using Application.Core.Validations;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Domain.OrderAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reports.Queries;

public class GetPaymentsSummaryReportHandler(IOrderReadRepository orderReadRepository)
    : IRequestHandler<GetPaymentsSummaryReportQuery, Result<PaymentsSummaryReportDto>>
{
    public async Task<Result<PaymentsSummaryReportDto>> Handle(
        GetPaymentsSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var rows = orderReadRepository.GetOrderReportRows();

        // Filtrar solo los tres estados de pago y agrupar en una sola pasada EN BD.
        // La proyeccion escalar de GetOrderReportRows() garantiza traduccion a SQL.
        var estadosPago = new[]
        {
            nameof(OrderStatus.PaymentRecieved),
            nameof(OrderStatus.PaymentFailed),
            nameof(OrderStatus.PaymentMismatch)
        };

        var agrupados = await rows
            .Where(r => estadosPago.Contains(r.OrderStatus))
            .GroupBy(r => r.OrderStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Amount = g.Sum(r => r.Total)
            })
            .ToListAsync(cancellationToken);

        // Extraer por estado. Si el estado no existe en el resultado, los valores quedan en 0.
        var confirmado = agrupados.FirstOrDefault(e => e.Status == nameof(OrderStatus.PaymentRecieved));
        var fallido    = agrupados.FirstOrDefault(e => e.Status == nameof(OrderStatus.PaymentFailed));
        var mismatch   = agrupados.FirstOrDefault(e => e.Status == nameof(OrderStatus.PaymentMismatch));

        var dto = new PaymentsSummaryReportDto
        {
            ConfirmedCount  = confirmado?.Count  ?? 0,
            ConfirmedAmount = confirmado?.Amount ?? 0,
            FailedCount     = fallido?.Count     ?? 0,
            FailedAmount    = fallido?.Amount    ?? 0,
            MismatchCount   = mismatch?.Count    ?? 0,
            MismatchAmount  = mismatch?.Amount   ?? 0
        };

        return Result<PaymentsSummaryReportDto>.Success(dto);
    }
}
