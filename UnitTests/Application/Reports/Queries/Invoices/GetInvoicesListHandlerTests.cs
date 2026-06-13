using Application.Billing.DTOs;
using Application.Core.Validations;
using Application.Interfaces.Services;
using Application.Reports.Queries.Invoices;

namespace UnitTests.Application.Reports.Queries.Invoices;

public class GetInvoicesListHandlerTests
{
    private static InvoiceListResult SamplePayload() => new(
        Items: new List<InvoiceSummary>
        {
            new(
                Id: "uuid-1",
                AccessKey: "ACC-1",
                Sequential: "000000001",
                Status: "COMPLETED",
                Total: 23m,
                IssueDate: DateTimeOffset.Parse("2026-06-12T02:23:10.305Z"),
                CreatedAt: DateTimeOffset.Parse("2026-06-12T02:23:10.305Z"),
                Customer: new InvoiceCustomerSummary("9999999999", "FINAL_CONSUMER", "Juan Diego"))
        },
        Meta: new InvoiceListMeta(Total: 1, Page: 1, Limit: 20, TotalPages: 1));

    [Fact]
    public async Task Handle_PassesThrough_FacadeResult_OnSuccess()
    {
        var facadeMock = new Mock<IBillingFacade>();
        var payload = SamplePayload();
        facadeMock
            .Setup(f => f.ListInvoicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvoiceListResult>.Success(payload));

        var handler = new GetInvoicesListHandler(facadeMock.Object);

        var result = await handler.Handle(new GetInvoicesListQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(payload);
    }

    [Fact]
    public async Task Handle_PassesThrough_FacadeResult_OnFailure()
    {
        var facadeMock = new Mock<IBillingFacade>();
        facadeMock
            .Setup(f => f.ListInvoicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvoiceListResult>.Failure("FacturaPlan returned 500", 500));

        var handler = new GetInvoicesListHandler(facadeMock.Object);

        var result = await handler.Handle(new GetInvoicesListQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(500);
        result.Error.Should().Contain("500");
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken_ToFacade()
    {
        var facadeMock = new Mock<IBillingFacade>();
        var empty = new InvoiceListResult(new List<InvoiceSummary>(), null);
        facadeMock
            .Setup(f => f.ListInvoicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvoiceListResult>.Success(empty));

        var handler = new GetInvoicesListHandler(facadeMock.Object);
        using var cts = new CancellationTokenSource();

        await handler.Handle(new GetInvoicesListQuery(), cts.Token);

        facadeMock.Verify(f => f.ListInvoicesAsync(cts.Token), Times.Once);
    }
}
