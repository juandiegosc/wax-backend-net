namespace Infrastructure.Billing;

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
    string? Timestamp);
