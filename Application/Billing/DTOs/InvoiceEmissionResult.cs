namespace Application.Billing.DTOs;

public sealed record InvoiceEmissionResult(
    bool Success,
    string? ExternalInvoiceId,
    string? AccessKey,
    string? Sequential,
    string? Status,
    string? ErrorMessage);
