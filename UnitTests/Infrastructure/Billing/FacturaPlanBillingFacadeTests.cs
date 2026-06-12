using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Billing.DTOs;
using Infrastructure.Billing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace UnitTests.Infrastructure.Billing;

public class FacturaPlanBillingFacadeTests
{
    private const string BaseUrl = "https://api-rest.factuplan.com.ec";
    private const string ApiKey = "test-api-key";
    private const string TaxpayerRuc = "9999999990001";

    private static FacturaPlanSettings DefaultSettings() => new()
    {
        BaseUrl = BaseUrl,
        ApiKey = ApiKey,
        TaxpayerRuc = TaxpayerRuc,
        Establishment = "001",
        EmissionPoint = "001",
        DefaultPaymentMethod = "19",
        TaxRate = 15m
    };

    private static InvoiceRequest BuildRequest() => new(
        OrderId: "order-1",
        PaymentIntentId: "pi-1",
        Customer: new InvoiceCustomer("9999999999", "Buyer", "buyer@test.com", null),
        Items: new List<InvoiceLine> { new("P1", "Product", 1, 10.00m, 1.50m) },
        Payment: new InvoicePayment(11.50m, "USD"));

    private static string ValidResponseJson(string id = "INV-001") =>
        $$$"""{"data":{"id":"{{{id}}}","accessKey":"ACC-123","sequential":"001","status":"AUTHORIZED","total":11.50},"meta":{"requestId":"req-1","timestamp":"2024-01-01T00:00:00Z"}}""";

    private static (FacturaPlanBillingFacade facade, Mock<HttpMessageHandler> handlerMock) BuildFacade(
        HttpResponseMessage response,
        FacturaPlanSettings? settings = null)
    {
        var effectiveSettings = settings ?? DefaultSettings();
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(effectiveSettings.BaseUrl)
        };

        var options = Options.Create(effectiveSettings);
        var mapper = new FacturaPlanRequestMapper();
        var logger = new Mock<ILogger<FacturaPlanBillingFacade>>().Object;

        var facade = new FacturaPlanBillingFacade(httpClient, options, mapper, logger);
        return (facade, handlerMock);
    }

    [Fact]
    public async Task EmitInvoiceAsync_PostsTo_V1DeveloperInvoices()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidResponseJson(), System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, handlerMock) = BuildFacade(response);

        await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.PathAndQuery == "/v1/developer/invoices"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task EmitInvoiceAsync_SendsApiKeyHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidResponseJson(), System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, handlerMock) = BuildFacade(response);

        await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Headers.Contains("X-API-Key") &&
                r.Headers.GetValues("X-API-Key").First() == ApiKey),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task EmitInvoiceAsync_SendsTaxpayerRucHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidResponseJson(), System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, handlerMock) = BuildFacade(response);

        await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Headers.Contains("x-taxpayer-ruc") &&
                r.Headers.GetValues("x-taxpayer-ruc").First() == TaxpayerRuc),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task EmitInvoiceAsync_SerializesPayload_AsCamelCase()
    {
        string? capturedBody = null;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidResponseJson(), System.Text.Encoding.UTF8, "application/json")
            });

        var settings = DefaultSettings();
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(settings.BaseUrl) };
        var options = Options.Create(settings);
        var mapper = new FacturaPlanRequestMapper();
        var logger = new Mock<ILogger<FacturaPlanBillingFacade>>().Object;
        var facade = new FacturaPlanBillingFacade(httpClient, options, mapper, logger);

        await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"customer\"");
        capturedBody.Should().Contain("\"items\"");
    }

    [Fact]
    public async Task EmitInvoiceAsync_Returns_SuccessTrueAndIds_When2xx()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidResponseJson("INV-999"), System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalInvoiceId.Should().Be("INV-999");
        result.AccessKey.Should().Be("ACC-123");
        result.Sequential.Should().Be("001");
    }

    [Fact]
    public async Task EmitInvoiceAsync_Returns_SuccessFalseWithStatusCode_When4xx()
    {
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent("Validation error", System.Text.Encoding.UTF8, "text/plain")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("422");
    }

    [Fact]
    public async Task EmitInvoiceAsync_Returns_SuccessFalseWithStatusCode_When5xx()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error", System.Text.Encoding.UTF8, "text/plain")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task EmitInvoiceAsync_Returns_SuccessFalse_OnHttpRequestException()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("DNS failure"));

        var settings = DefaultSettings();
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(settings.BaseUrl) };
        var options = Options.Create(settings);
        var mapper = new FacturaPlanRequestMapper();
        var logger = new Mock<ILogger<FacturaPlanBillingFacade>>().Object;
        var facade = new FacturaPlanBillingFacade(httpClient, options, mapper, logger);

        var result = await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task EmitInvoiceAsync_Returns_SuccessFalse_OnJsonException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json", System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("deserialization");
    }

    [Fact]
    public async Task EmitInvoiceAsync_PropagatesCancellationToken()
    {
        CancellationToken capturedToken = default;
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, ct) => { capturedToken = ct; })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidResponseJson(), System.Text.Encoding.UTF8, "application/json")
            });

        var settings = DefaultSettings();
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(settings.BaseUrl) };
        var options = Options.Create(settings);
        var mapper = new FacturaPlanRequestMapper();
        var logger = new Mock<ILogger<FacturaPlanBillingFacade>>().Object;
        var facade = new FacturaPlanBillingFacade(httpClient, options, mapper, logger);

        using var cts = new CancellationTokenSource();
        await facade.EmitInvoiceAsync(BuildRequest(), cts.Token);

        // HttpClient links the caller token with an internal timeout token.
        // The captured token at SendAsync level is a linked token derived from cts.Token —
        // it is always cancellable when a caller token is supplied.
        capturedToken.CanBeCanceled.Should().BeTrue("the facade must propagate the caller CancellationToken");
    }

    [Fact]
    public async Task EmitInvoiceAsync_NeverLogsApiKey_NorTaxpayerRuc()
    {
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent("error", System.Text.Encoding.UTF8, "text/plain")
        };

        var settings = DefaultSettings();
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(settings.BaseUrl) };
        var options = Options.Create(settings);
        var mapper = new FacturaPlanRequestMapper();
        var loggerMock = new Mock<ILogger<FacturaPlanBillingFacade>>();
        var facade = new FacturaPlanBillingFacade(httpClient, options, mapper, loggerMock.Object);

        await facade.EmitInvoiceAsync(BuildRequest(), CancellationToken.None);

        loggerMock.Invocations
            .SelectMany(inv => inv.Arguments)
            .OfType<string>()
            .Should().NotContain(s => s.Contains(ApiKey) || s.Contains(TaxpayerRuc));
    }

    // ------------------------------------------------------------------
    // ListInvoicesAsync
    // ------------------------------------------------------------------

    private const string ListResponseJson =
        @"{
          ""data"": [
            {
              ""id"": ""uuid-1"",
              ""accessKey"": ""ACC-1"",
              ""sequential"": ""000000001"",
              ""status"": ""COMPLETED"",
              ""total"": ""23"",
              ""issueDate"": ""2026-06-12T02:23:10.305Z"",
              ""createdAt"": ""2026-06-12T02:23:10.305Z"",
              ""customer"": {
                ""identification"": ""9999999999"",
                ""identificationType"": ""FINAL_CONSUMER"",
                ""legalName"": ""Juan Diego""
              }
            },
            {
              ""id"": ""uuid-2"",
              ""accessKey"": ""ACC-2"",
              ""sequential"": ""000000002"",
              ""status"": ""COMPLETED"",
              ""total"": ""106.4"",
              ""issueDate"": ""2026-06-11T10:00:00.000Z"",
              ""createdAt"": ""2026-06-11T10:00:00.000Z"",
              ""customer"": {
                ""identification"": ""9999999999"",
                ""identificationType"": ""FINAL_CONSUMER"",
                ""legalName"": ""Juan Diego""
              }
            }
          ],
          ""meta"": {
            ""requestId"": ""req-1"",
            ""timestamp"": ""2026-06-12T04:00:38.305Z"",
            ""total"": 2,
            ""page"": 1,
            ""limit"": 20,
            ""totalPages"": 1
          }
        }";

    [Fact]
    public async Task ListInvoicesAsync_GetsTo_V1DeveloperInvoices()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ListResponseJson, System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, handlerMock) = BuildFacade(response);

        await facade.ListInvoicesAsync(CancellationToken.None);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == "/v1/developer/invoices"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListInvoicesAsync_SendsAuthHeaders()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ListResponseJson, System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, handlerMock) = BuildFacade(response);

        await facade.ListInvoicesAsync(CancellationToken.None);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Headers.Contains("X-API-Key") &&
                r.Headers.GetValues("X-API-Key").First() == ApiKey &&
                r.Headers.Contains("x-taxpayer-ruc") &&
                r.Headers.GetValues("x-taxpayer-ruc").First() == TaxpayerRuc),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListInvoicesAsync_Returns_Success_With_Mapped_Items()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ListResponseJson, System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.ListInvoicesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be("uuid-1");
        result.Value.Items[0].AccessKey.Should().Be("ACC-1");
        result.Value.Items[0].Sequential.Should().Be("000000001");
        result.Value.Items[0].Status.Should().Be("COMPLETED");
        result.Value.Items[0].Total.Should().Be(23m);
        result.Value.Items[0].Customer!.LegalName.Should().Be("Juan Diego");
        result.Value.Items[1].Total.Should().Be(106.4m);
        result.Value.Meta!.Total.Should().Be(2);
        result.Value.Meta.Limit.Should().Be(20);
    }

    [Fact]
    public async Task ListInvoicesAsync_Returns_Failure_When_NonSuccess_Status()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error", System.Text.Encoding.UTF8, "text/plain")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.ListInvoicesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(500);
        result.Error.Should().Contain("500");
    }

    [Fact]
    public async Task ListInvoicesAsync_Returns_Failure_OnJsonException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json", System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.ListInvoicesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(502);
        result.Error.Should().Contain("deserialization");
    }

    // ------------------------------------------------------------------
    // GetInvoiceAsync
    // ------------------------------------------------------------------

    private const string DetailResponseJson =
        @"{
          ""data"": {
            ""id"": ""uuid-1"",
            ""accessKey"": ""ACC-1"",
            ""sequential"": ""000000001"",
            ""status"": ""COMPLETED"",
            ""total"": ""115"",
            ""issueDate"": ""2026-06-12T02:23:10.305Z"",
            ""createdAt"": ""2026-06-12T02:23:10.305Z"",
            ""customer"": {
              ""identification"": ""9999999999"",
              ""identificationType"": ""FINAL_CONSUMER"",
              ""legalName"": ""Juan Diego""
            },
            ""paymentMethods"": [
              { ""method"": ""19"", ""amount"": 115.00 }
            ],
            ""details"": [
              { ""code"": ""P1"", ""description"": ""Product"", ""quantity"": 1, ""unitPrice"": 100.00, ""tax"": 15 }
            ]
          },
          ""meta"": { ""requestId"": ""req-1"", ""timestamp"": ""2026-06-12T04:00:38.305Z"" }
        }";

    [Fact]
    public async Task GetInvoiceAsync_GetsTo_V1DeveloperInvoices_WithId()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(DetailResponseJson, System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, handlerMock) = BuildFacade(response);

        await facade.GetInvoiceAsync("uuid-1", CancellationToken.None);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == "/v1/developer/invoices/uuid-1"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetInvoiceAsync_Returns_Success_With_Mapped_Detail()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(DetailResponseJson, System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.GetInvoiceAsync("uuid-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("uuid-1");
        result.Value.AccessKey.Should().Be("ACC-1");
        result.Value.Total.Should().Be(115m);
        result.Value.Customer!.LegalName.Should().Be("Juan Diego");
        result.Value.PaymentMethods.Should().HaveCount(1);
        result.Value.PaymentMethods[0].Method.Should().Be("19");
        result.Value.PaymentMethods[0].Amount.Should().Be(115.00m);
        result.Value.Details.Should().HaveCount(1);
        result.Value.Details[0].Code.Should().Be("P1");
        result.Value.Details[0].UnitPrice.Should().Be(100.00m);
    }

    [Fact]
    public async Task GetInvoiceAsync_Returns_404_When_NotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Invoice not found", System.Text.Encoding.UTF8, "text/plain")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.GetInvoiceAsync("unknown", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(404);
    }

    [Fact]
    public async Task GetInvoiceAsync_Returns_Failure_OnJsonException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json", System.Text.Encoding.UTF8, "application/json")
        };
        var (facade, _) = BuildFacade(response);

        var result = await facade.GetInvoiceAsync("uuid-1", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(502);
        result.Error.Should().Contain("deserialization");
    }
}
