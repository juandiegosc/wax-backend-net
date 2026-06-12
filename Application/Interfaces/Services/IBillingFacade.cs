using Application.Billing.DTOs;
using Application.Core.Validations;

namespace Application.Interfaces.Services;

public interface IBillingFacade
{
    Task<InvoiceEmissionResult> EmitInvoiceAsync(
        InvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<InvoiceListResult>> ListInvoicesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<InvoiceDetail>> GetInvoiceAsync(
        string id,
        CancellationToken cancellationToken = default);
}
