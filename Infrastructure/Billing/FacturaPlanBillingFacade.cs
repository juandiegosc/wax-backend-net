using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Billing.DTOs;
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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
}
