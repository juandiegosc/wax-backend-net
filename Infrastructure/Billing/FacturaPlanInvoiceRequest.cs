namespace Infrastructure.Billing;

internal sealed record FacturaPlanInvoiceRequest(
    FacturaPlanCustomer Customer,
    IReadOnlyList<FacturaPlanItem> Items,
    IReadOnlyList<FacturaPlanPayment> Payments,
    string Establishment,
    string EmissionPoint,
    bool SendEmail);

internal sealed record FacturaPlanCustomer(
    string IdentificationType,
    string Identification,
    string LegalName,
    string? Email,
    string? Address);

internal sealed record FacturaPlanItem(
    int Quantity,
    string Code,
    string Description,
    decimal UnitPrice,
    string TaxType,
    decimal Tax);

internal sealed record FacturaPlanPayment(
    string Method,
    decimal Amount);
