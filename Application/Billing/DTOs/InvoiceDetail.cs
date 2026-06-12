namespace Application.Billing.DTOs;

public sealed record InvoiceDetail(
    string Id,
    string? AccessKey,
    string? Sequential,
    string? Status,
    decimal? Total,
    DateTimeOffset? IssueDate,
    DateTimeOffset? CreatedAt,
    InvoiceCustomerSummary? Customer,
    IReadOnlyList<InvoiceDetailPaymentMethod> PaymentMethods,
    IReadOnlyList<InvoiceDetailLine> Details);

public sealed record InvoiceDetailPaymentMethod(
    string? Method,
    decimal? Amount);

public sealed record InvoiceDetailLine(
    string? Code,
    string? Description,
    int? Quantity,
    decimal? UnitPrice,
    decimal? Tax);
