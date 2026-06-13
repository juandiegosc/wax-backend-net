using Application.Billing.DTOs;
using Application.Core.Validations;
using MediatR;

namespace Application.Reports.Queries.Invoices;

public record GetInvoiceByIdQuery(string Id) : IRequest<Result<InvoiceDetail>>;
