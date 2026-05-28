using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.OrderAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetPaymentsSummaryReportHandlerTests
{
    private static Mock<IOrderReadRepository> SetupMock(List<OrderReportRow> rows)
    {
        var mock = new Mock<IOrderReadRepository>();
        mock.Setup(r => r.GetOrderReportRows(null, null))
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-09-B: sin pagos fallidos ni mismatch
    [Fact]
    public async Task Handle_SinOrdenes_RetornaTodosConteosCero()
    {
        var repoMock = SetupMock([]);
        var handler = new GetPaymentsSummaryReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetPaymentsSummaryReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.ConfirmedCount.Should().Be(0);
        resultado.Value.ConfirmedAmount.Should().Be(0);
        resultado.Value.FailedCount.Should().Be(0);
        resultado.Value.FailedAmount.Should().Be(0);
        resultado.Value.MismatchCount.Should().Be(0);
        resultado.Value.MismatchAmount.Should().Be(0);
    }

    // RFC-09-A: resumen de pagos mixto
    [Fact]
    public async Task Handle_ConPagosMixtos_RetornaConteosYMontosPorEstado()
    {
        var rows = new List<OrderReportRow>
        {
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 2000, BuyerEmail = "a@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 3000, BuyerEmail = "b@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1500, BuyerEmail = "c@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentFailed),   Total = 500,  BuyerEmail = "d@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentMismatch), Total = 100,  BuyerEmail = "e@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentMismatch), Total = 200,  BuyerEmail = "f@test.com", CreatedAt = DateTime.UtcNow },
            // Estado distinto — no debe afectar el resumen de pagos
            new() { OrderStatus = nameof(OrderStatus.Pending),         Total = 999,  BuyerEmail = "g@test.com", CreatedAt = DateTime.UtcNow },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetPaymentsSummaryReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetPaymentsSummaryReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.ConfirmedCount.Should().Be(3);
        resultado.Value.ConfirmedAmount.Should().Be(6500);
        resultado.Value.FailedCount.Should().Be(1);
        resultado.Value.FailedAmount.Should().Be(500);
        resultado.Value.MismatchCount.Should().Be(2);
        resultado.Value.MismatchAmount.Should().Be(300);
    }

    // Solo confirmados — FailedCount y MismatchCount deben ser 0
    [Fact]
    public async Task Handle_SoloPagosConfirmados_RetornaFailedYMismatchEnCero()
    {
        var rows = new List<OrderReportRow>
        {
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 5000, BuyerEmail = "a@test.com", CreatedAt = DateTime.UtcNow },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetPaymentsSummaryReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetPaymentsSummaryReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.ConfirmedCount.Should().Be(1);
        resultado.Value.ConfirmedAmount.Should().Be(5000);
        resultado.Value.FailedCount.Should().Be(0);
        resultado.Value.FailedAmount.Should().Be(0);
        resultado.Value.MismatchCount.Should().Be(0);
        resultado.Value.MismatchAmount.Should().Be(0);
    }
}
