using Application.Billing.DTOs;
using Application.Core.Validations;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Reports.Queries.Invoices;

public class GetInvoiceByIdHandler(IBillingFacade billingFacade)
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetail>>
{
    public Task<Result<InvoiceDetail>> Handle(
        GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
        => billingFacade.GetInvoiceAsync(request.Id, cancellationToken);
}
