namespace Infrastructure.Billing;

// Envelope comun de FacturaPlan: { data, meta }. El POST devuelve data=objeto;
// el GET de listado devuelve data=array; el GET por id devuelve data=objeto.
internal sealed record FacturaPlanInvoiceResponse(
    FacturaPlanInvoiceData? Data,
    FacturaPlanResponseMeta? Meta);

internal sealed record FacturaPlanInvoiceData(
    string Id,
    string? AccessKey,
    string? Sequential,
    string? Status,
    decimal Total);

internal sealed record FacturaPlanResponseMeta(
    string? RequestId,
    string? Timestamp,
    int? Total,
    int? Page,
    int? Limit,
    int? TotalPages);

internal sealed record FacturaPlanInvoiceListEnvelope(
    IReadOnlyList<FacturaPlanInvoiceListItem>? Data,
    FacturaPlanResponseMeta? Meta);

internal sealed record FacturaPlanInvoiceListItem(
    string Id,
    string? AccessKey,
    string? Sequential,
    string? Status,
    decimal? Total,
    DateTimeOffset? IssueDate,
    DateTimeOffset? CreatedAt,
    FacturaPlanCustomerSummary? Customer);

internal sealed record FacturaPlanCustomerSummary(
    string? Identification,
    string? IdentificationType,
    string? LegalName);

internal sealed record FacturaPlanInvoiceDetailEnvelope(
    FacturaPlanInvoiceDetailResponse? Data,
    FacturaPlanResponseMeta? Meta);

internal sealed record FacturaPlanInvoiceDetailResponse(
    string Id,
    string? AccessKey,
    string? Sequential,
    string? Status,
    decimal? Total,
    DateTimeOffset? IssueDate,
    DateTimeOffset? CreatedAt,
    FacturaPlanCustomerSummary? Customer,
    IReadOnlyList<FacturaPlanPaymentMethod>? PaymentMethods,
    IReadOnlyList<FacturaPlanInvoiceDetailLine>? Details);

internal sealed record FacturaPlanPaymentMethod(
    string? Method,
    decimal? Amount);

internal sealed record FacturaPlanInvoiceDetailLine(
    string? Code,
    string? Description,
    int? Quantity,
    decimal? UnitPrice,
    decimal? Tax);
