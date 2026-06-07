using Application.Billing.DTOs;
using Application.IntegrationEvents.BillingEvents;
using Application.Interfaces.Services;
using Infrastructure.Messaging.Consumers.BillingConsumers;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace UnitTests.Infrastructure.Messaging.Consumers.BillingConsumers;

public class OrderBillingRequestedConsumerTests
{
    private readonly Mock<IBillingFacade> _facade = new();
    private readonly Mock<ILogger<OrderBillingRequestedConsumer>> _logger = new();

    private static string BuildOrderItemsJson(string name = "Test Product", string productId = "prod-1",
        long price = 5000L, int quantity = 2) =>
        JsonSerializer.Serialize(new[]
        {
            new { Name = name, ProductId = productId, Price = price, Quantity = quantity }
        });

    private static OrderBillingRequestedIntegrationEvent CreateEvent(
        string? billingName = null,
        string buyerEmail = "buyer@test.com",
        long total = 10000L,
        long subtotal = 9000L,
        long deliveryFee = 1000L,
        string? orderItemsJson = null) => new()
    {
        OrderId = "order-1",
        BuyerEmail = buyerEmail,
        BillingName = billingName,
        Subtotal = subtotal,
        DeliveryFee = deliveryFee,
        Total = total,
        PaymentIntentId = "pi-1",
        OrderItems = orderItemsJson ?? BuildOrderItemsJson(),
        OccurredAt = DateTime.UtcNow
    };

    private Mock<ConsumeContext<OrderBillingRequestedIntegrationEvent>> CreateContext(
        OrderBillingRequestedIntegrationEvent? evt = null,
        CancellationToken ct = default)
    {
        var contextMock = new Mock<ConsumeContext<OrderBillingRequestedIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt ?? CreateEvent());
        contextMock.Setup(c => c.CancellationToken).Returns(ct);
        return contextMock;
    }

    private OrderBillingRequestedConsumer BuildConsumer() =>
        new(_facade.Object, _logger.Object);

    [Fact]
    public async Task Consume_CallsFacadeOnce_WhenMessageIsValid()
    {
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceEmissionResult(true, "INV-001", "ACC-1", "001", "AUTHORIZED", null));

        var consumer = BuildConsumer();
        var context = CreateContext();

        await consumer.Consume(context.Object);

        _facade.Verify(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_Throws_WhenFacadeReturnsSuccessFalse()
    {
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceEmissionResult(false, null, null, null, null, "Invoice failed"));

        var consumer = BuildConsumer();
        var context = CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context.Object));
    }

    [Fact]
    public async Task Consume_Propagates_WhenFacadeThrows()
    {
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var consumer = BuildConsumer();
        var context = CreateContext();

        await Assert.ThrowsAsync<HttpRequestException>(() => consumer.Consume(context.Object));
    }

    [Fact]
    public async Task Consume_UsesBuyerEmailAsLegalName_WhenBillingNameIsNull()
    {
        InvoiceRequest? capturedRequest = null;
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new InvoiceEmissionResult(true, "INV-001", null, null, null, null));

        var consumer = BuildConsumer();
        var evt = CreateEvent(billingName: null, buyerEmail: "buyer@test.com");
        var context = CreateContext(evt);

        await consumer.Consume(context.Object);

        capturedRequest!.Customer.LegalName.Should().Be("buyer@test.com");
    }

    [Fact]
    public async Task Consume_UsesBillingName_WhenPresent()
    {
        InvoiceRequest? capturedRequest = null;
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new InvoiceEmissionResult(true, "INV-001", null, null, null, null));

        var consumer = BuildConsumer();
        var evt = CreateEvent(billingName: "John Doe", buyerEmail: "john@test.com");
        var context = CreateContext(evt);

        await consumer.Consume(context.Object);

        capturedRequest!.Customer.LegalName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Consume_ConvertsCentavosToDecimal_BeforeCallingFacade()
    {
        InvoiceRequest? capturedRequest = null;
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new InvoiceEmissionResult(true, "INV-001", null, null, null, null));

        var consumer = BuildConsumer();
        var evt = CreateEvent(total: 10000L);
        var context = CreateContext(evt);

        await consumer.Consume(context.Object);

        capturedRequest!.Payment.Amount.Should().Be(100.00m);
    }

    [Fact]
    public async Task Consume_DeserializesOrderItemsJson_Correctly()
    {
        InvoiceRequest? capturedRequest = null;
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new InvoiceEmissionResult(true, "INV-001", null, null, null, null));

        var consumer = BuildConsumer();
        var itemsJson = BuildOrderItemsJson("Special Item", "sp-prod", 3000L, 1);
        var evt = CreateEvent(orderItemsJson: itemsJson, total: 3000L, subtotal: 3000L, deliveryFee: 0L);
        var context = CreateContext(evt);

        await consumer.Consume(context.Object);

        capturedRequest!.Items.Should().HaveCountGreaterThanOrEqualTo(1);
        capturedRequest.Items[0].Description.Should().Be("Special Item");
        capturedRequest.Items[0].Code.Should().Be("sp-prod");
    }

    [Fact]
    public async Task Consume_PassesCancellationToken_ToFacade()
    {
        CancellationToken capturedToken = default;
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceRequest, CancellationToken>((_, ct) => capturedToken = ct)
            .ReturnsAsync(new InvoiceEmissionResult(true, "INV-001", null, null, null, null));

        var consumer = BuildConsumer();
        using var cts = new CancellationTokenSource();
        var context = CreateContext(ct: cts.Token);

        await consumer.Consume(context.Object);

        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Consume_LogsExternalInvoiceId_OnSuccess()
    {
        var loggerMock = new Mock<ILogger<OrderBillingRequestedConsumer>>();
        _facade
            .Setup(f => f.EmitInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceEmissionResult(true, "INV-001", "ACC-1", "001", "AUTHORIZED", null));

        var consumer = new OrderBillingRequestedConsumer(_facade.Object, loggerMock.Object);
        var context = CreateContext();

        await consumer.Consume(context.Object);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("INV-001")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
