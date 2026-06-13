using Application.Billing.DTOs;
using Application.Core.Validations;
using Application.Interfaces.Services;
using Application.Reports.Queries.Invoices;

namespace UnitTests.Application.Reports.Queries.Invoices;

public class GetInvoiceByIdHandlerTests
{
    private static InvoiceDetail SampleDetail() => new(
        Id: "uuid-1",
        AccessKey: "ACC-1",
        Sequential: "000000001",
        Status: "COMPLETED",
        Total: 115m,
        IssueDate: DateTimeOffset.Parse("2026-06-12T02:23:10.305Z"),
        CreatedAt: DateTimeOffset.Parse("2026-06-12T02:23:10.305Z"),
        Customer: new InvoiceCustomerSummary("9999999999", "FINAL_CONSUMER", "Juan Diego"),
        PaymentMethods: new List<InvoiceDetailPaymentMethod>
        {
            new("19", 115.00m)
        },
        Details: new List<InvoiceDetailLine>
        {
            new("P1", "Product", 1, 100.00m, 15m)
        });

    [Fact]
    public async Task Handle_PassesThrough_FacadeResult_OnSuccess()
    {
        var facadeMock = new Mock<IBillingFacade>();
        var payload = SampleDetail();
        facadeMock
            .Setup(f => f.GetInvoiceAsync("uuid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvoiceDetail>.Success(payload));

        var handler = new GetInvoiceByIdHandler(facadeMock.Object);

        var result = await handler.Handle(new GetInvoiceByIdQuery("uuid-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(payload);
    }

    [Fact]
    public async Task Handle_PassesThrough_404_OnNotFound()
    {
        var facadeMock = new Mock<IBillingFacade>();
        facadeMock
            .Setup(f => f.GetInvoiceAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvoiceDetail>.Failure("FacturaPlan returned 404", 404));

        var handler = new GetInvoiceByIdHandler(facadeMock.Object);

        var result = await handler.Handle(new GetInvoiceByIdQuery("unknown"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(404);
    }

    [Fact]
    public async Task Handle_PassesIdToFacade()
    {
        var facadeMock = new Mock<IBillingFacade>();
        facadeMock
            .Setup(f => f.GetInvoiceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvoiceDetail>.Success(SampleDetail()));

        var handler = new GetInvoiceByIdHandler(facadeMock.Object);

        await handler.Handle(new GetInvoiceByIdQuery("uuid-42"), CancellationToken.None);

        facadeMock.Verify(f => f.GetInvoiceAsync("uuid-42", It.IsAny<CancellationToken>()), Times.Once);
    }
}
