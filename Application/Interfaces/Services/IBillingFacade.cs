using Application.Billing.DTOs;

namespace Application.Interfaces.Services;

public interface IBillingFacade
{
    Task<InvoiceEmissionResult> EmitInvoiceAsync(
        InvoiceRequest request,
        CancellationToken cancellationToken = default);
}
