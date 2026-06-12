namespace Application.Billing.DTOs;

public sealed record InvoiceSummary(
    string Id,
    string? AccessKey,
    string? Sequential,
    string? Status,
    decimal? Total,
    DateTimeOffset? IssueDate,
    DateTimeOffset? CreatedAt,
    InvoiceCustomerSummary? Customer);

public sealed record InvoiceCustomerSummary(
    string? Identification,
    string? IdentificationType,
    string? LegalName);

public sealed record InvoiceListResult(
    IReadOnlyList<InvoiceSummary> Items,
    InvoiceListMeta? Meta);

// FacturaPlan pagina por defecto (limit=20) aunque no lo documenta.
public sealed record InvoiceListMeta(
    int? Total,
    int? Page,
    int? Limit,
    int? TotalPages);
