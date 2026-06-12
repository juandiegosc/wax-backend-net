using Application.Billing.DTOs;
using Application.Core.Validations;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Reports.Queries.Invoices;

// Pass-through a FacturaPlan: el contrato de listado pagina por defecto (limit=20)
// y no expone filtros documentados — reenviamos lista + meta tal cual.
public class GetInvoicesListHandler(IBillingFacade billingFacade)
    : IRequestHandler<GetInvoicesListQuery, Result<InvoiceListResult>>
{
    public Task<Result<InvoiceListResult>> Handle(
        GetInvoicesListQuery request,
        CancellationToken cancellationToken)
        => billingFacade.ListInvoicesAsync(cancellationToken);
}
