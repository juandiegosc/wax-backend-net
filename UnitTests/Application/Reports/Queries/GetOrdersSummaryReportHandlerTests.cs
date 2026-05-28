using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.OrderAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetOrdersSummaryReportHandlerTests
{
    private static Mock<IOrderReadRepository> SetupMock(List<OrderReportRow> rows)
    {
        var mock = new Mock<IOrderReadRepository>();
        mock.Setup(r => r.GetOrderReportRows(null, null))
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-05-A: sin ordenes
    [Fact]
    public async Task Handle_SinOrdenes_RetornaConteosCero()
    {
        var repoMock = SetupMock([]);
        var handler = new GetOrdersSummaryReportHandler(repoMock.Object);

        var result = await handler.Handle(new GetOrdersSummaryReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOrders.Should().Be(0);
        result.Value.TotalRevenue.Should().Be(0);
        result.Value.AverageTicket.Should().Be(0);
        result.Value.ByStatus.Should().AllSatisfy(s => s.Count.Should().Be(0));
    }

    // RFC-05-B: ordenes en multiples estados
    [Fact]
    public async Task Handle_OrdenesEnMultiplesEstados_RetornaConteoYRevenueCorrectos()
    {
        var rows = new List<OrderReportRow>
        {
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1000, BuyerEmail = "a@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1000, BuyerEmail = "b@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1000, BuyerEmail = "c@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1000, BuyerEmail = "d@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1000, BuyerEmail = "e@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentFailed),   Total = 500,  BuyerEmail = "f@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentFailed),   Total = 500,  BuyerEmail = "g@test.com", CreatedAt = DateTime.UtcNow },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetOrdersSummaryReportHandler(repoMock.Object);

        var result = await handler.Handle(new GetOrdersSummaryReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOrders.Should().Be(7);
        result.Value.TotalRevenue.Should().Be(6000);
        result.Value.AverageTicket.Should().Be(857);

        var recibidas = result.Value.ByStatus.FirstOrDefault(s => s.Status == nameof(OrderStatus.PaymentRecieved));
        recibidas.Should().NotBeNull();
        recibidas!.Count.Should().Be(5);

        var fallidas = result.Value.ByStatus.FirstOrDefault(s => s.Status == nameof(OrderStatus.PaymentFailed));
        fallidas.Should().NotBeNull();
        fallidas!.Count.Should().Be(2);
    }
}
