namespace Application.Billing.DTOs;

public sealed record InvoiceRequest(
    string OrderId,
    string PaymentIntentId,
    InvoiceCustomer Customer,
    IReadOnlyList<InvoiceLine> Items,
    InvoicePayment Payment);

public sealed record InvoiceCustomer(
    string Identification,
    string LegalName,
    string? Email,
    string? Address);

public sealed record InvoiceLine(
    string Code,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Tax);

public sealed record InvoicePayment(
    decimal Amount,
    string Currency);
