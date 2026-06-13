using Application.Billing.DTOs;
using Application.Core.Validations;
using MediatR;

namespace Application.Reports.Queries.Invoices;

public class GetInvoicesListQuery : IRequest<Result<InvoiceListResult>>
{
}
