using Application.Orders.DTOs;
using Application.Reports.DTOs;

namespace Application.Interfaces.Repositories.ReadRepositories;

public interface IOrderReadRepository
{
    Task<OrderDto?> GetOrderByIdAsync(string id,  CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default);
    IQueryable<OrderDto> GetQueryable(string? statusFilter = null, string? userId = null);

    /// <summary>
    /// Proyeccion escalar SQL-traducible para reportes.
    /// No incluye deserializacion de OrderItems (JSON), por lo que GroupBy/Sum/Count
    /// se ejecutan en PostgreSQL y no materializan filas en memoria del cliente.
    /// </summary>
    IQueryable<OrderReportRow> GetOrderReportRows(DateTime? from = null, DateTime? to = null);
}