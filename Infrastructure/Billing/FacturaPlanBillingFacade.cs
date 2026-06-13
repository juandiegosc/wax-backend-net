using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Billing.DTOs;
using Application.Core.Validations;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Billing;

public class FacturaPlanBillingFacade(
    HttpClient httpClient,
    IOptions<FacturaPlanSettings> settings,
    FacturaPlanRequestMapper mapper,
    ILogger<FacturaPlanBillingFacade> logger) : IBillingFacade
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // FacturaPlan devuelve montos como string ("23", "106.4"); este flag permite
        // deserializarlos directo a decimal sin custom converter.
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<InvoiceEmissionResult> EmitInvoiceAsync(
        InvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var s = settings.Value;
        var facturaRequest = mapper.Map(request, s);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/developer/invoices")
        {
            Content = JsonContent.Create(facturaRequest, options: JsonOptions)
        };

        httpRequest.Headers.Add("X-API-Key", s.ApiKey);
        httpRequest.Headers.Add("x-taxpayer-ruc", s.TaxpayerRuc);

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var invoiceResponse = JsonSerializer.Deserialize<FacturaPlanInvoiceResponse>(json, JsonOptions);
                    var data = invoiceResponse?.Data;

                    return new InvoiceEmissionResult(
                        Success: true,
                        ExternalInvoiceId: data?.Id,
                        AccessKey: data?.AccessKey,
                        Sequential: data?.Sequential,
                        Status: data?.Status,
                        ErrorMessage: null);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Response deserialization failed for invoice emission");
                    return new InvoiceEmissionResult(
                        Success: false,
                        ExternalInvoiceId: null,
                        AccessKey: null,
                        Sequential: null,
                        Status: null,
                        ErrorMessage: "Response deserialization failed: " + ex.Message);
                }
            }
            else
            {
                var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "FacturaPlan returned non-success status {StatusCode}. Body (truncated): {Body}",
                    (int)response.StatusCode,
                    rawBody.Length > 500 ? rawBody[..500] : rawBody);

                return new InvoiceEmissionResult(
                    Success: false,
                    ExternalInvoiceId: null,
                    AccessKey: null,
                    Sequential: null,
                    Status: null,
                    ErrorMessage: $"{(int)response.StatusCode}: {rawBody}");
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP request to FacturaPlan failed");
            return new InvoiceEmissionResult(
                Success: false,
                ExternalInvoiceId: null,
                AccessKey: null,
                Sequential: null,
                Status: null,
                ErrorMessage: ex.Message);
        }
    }

    public async Task<Result<InvoiceListResult>> ListInvoicesAsync(
        CancellationToken cancellationToken = default)
    {
        var s = settings.Value;
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/v1/developer/invoices");
        httpRequest.Headers.Add("X-API-Key", s.ApiKey);
        httpRequest.Headers.Add("x-taxpayer-ruc", s.TaxpayerRuc);

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "FacturaPlan list returned non-success status {StatusCode}. Body (truncated): {Body}",
                    (int)response.StatusCode,
                    rawBody.Length > 500 ? rawBody[..500] : rawBody);

                return Result<InvoiceListResult>.Failure(
                    $"FacturaPlan returned {(int)response.StatusCode}",
                    (int)response.StatusCode);
            }

            try
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var envelope = JsonSerializer.Deserialize<FacturaPlanInvoiceListEnvelope>(json, JsonOptions);
                var raw = envelope?.Data ?? Array.Empty<FacturaPlanInvoiceListItem>();
                IReadOnlyList<InvoiceSummary> items = raw.Select(MapToSummary).ToList();
                var meta = MapToListMeta(envelope?.Meta);
                return Result<InvoiceListResult>.Success(new InvoiceListResult(items, meta));
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Response deserialization failed for invoice list");
                return Result<InvoiceListResult>.Failure(
                    "Response deserialization failed: " + ex.Message,
                    502);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP request to FacturaPlan list failed");
            return Result<InvoiceListResult>.Failure(ex.Message, 502);
        }
    }

    public async Task<Result<InvoiceDetail>> GetInvoiceAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var s = settings.Value;
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/v1/developer/invoices/{id}");
        httpRequest.Headers.Add("X-API-Key", s.ApiKey);
        httpRequest.Headers.Add("x-taxpayer-ruc", s.TaxpayerRuc);

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "FacturaPlan get returned non-success status {StatusCode}. Body (truncated): {Body}",
                    (int)response.StatusCode,
                    rawBody.Length > 500 ? rawBody[..500] : rawBody);

                return Result<InvoiceDetail>.Failure(
                    $"FacturaPlan returned {(int)response.StatusCode}",
                    (int)response.StatusCode);
            }

            try
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var envelope = JsonSerializer.Deserialize<FacturaPlanInvoiceDetailEnvelope>(json, JsonOptions);
                if (envelope?.Data is null)
                {
                    return Result<InvoiceDetail>.Failure("Empty response from FacturaPlan", 502);
                }

                return Result<InvoiceDetail>.Success(MapToDetail(envelope.Data));
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Response deserialization failed for invoice detail");
                return Result<InvoiceDetail>.Failure(
                    "Response deserialization failed: " + ex.Message,
                    502);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP request to FacturaPlan get failed");
            return Result<InvoiceDetail>.Failure(ex.Message, 502);
        }
    }

    private static InvoiceSummary MapToSummary(FacturaPlanInvoiceListItem item) => new(
        Id: item.Id,
        AccessKey: item.AccessKey,
        Sequential: item.Sequential,
        Status: item.Status,
        Total: item.Total,
        IssueDate: item.IssueDate,
        CreatedAt: item.CreatedAt,
        Customer: MapCustomer(item.Customer));

    private static InvoiceCustomerSummary? MapCustomer(FacturaPlanCustomerSummary? c) =>
        c is null
            ? null
            : new InvoiceCustomerSummary(c.Identification, c.IdentificationType, c.LegalName);

    private static InvoiceListMeta? MapToListMeta(FacturaPlanResponseMeta? meta) =>
        meta is null
            ? null
            : new InvoiceListMeta(meta.Total, meta.Page, meta.Limit, meta.TotalPages);

    private static InvoiceDetail MapToDetail(FacturaPlanInvoiceDetailResponse raw)
    {
        IReadOnlyList<InvoiceDetailPaymentMethod> payments = raw.PaymentMethods is null
            ? Array.Empty<InvoiceDetailPaymentMethod>()
            : raw.PaymentMethods
                .Select(p => new InvoiceDetailPaymentMethod(p.Method, p.Amount))
                .ToList();

        IReadOnlyList<InvoiceDetailLine> details = raw.Details is null
            ? Array.Empty<InvoiceDetailLine>()
            : raw.Details
                .Select(d => new InvoiceDetailLine(d.Code, d.Description, d.Quantity, d.UnitPrice, d.Tax))
                .ToList();

        return new InvoiceDetail(
            Id: raw.Id,
            AccessKey: raw.AccessKey,
            Sequential: raw.Sequential,
            Status: raw.Status,
            Total: raw.Total,
            IssueDate: raw.IssueDate,
            CreatedAt: raw.CreatedAt,
            Customer: MapCustomer(raw.Customer),
            PaymentMethods: payments,
            Details: details);
    }
}
