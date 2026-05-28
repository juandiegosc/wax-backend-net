using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.OrderAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetOrdersByDateReportHandlerTests
{
    private static Mock<IOrderReadRepository> SetupMock(
        List<OrderReportRow> rows,
        DateTime? from = null,
        DateTime? to = null)
    {
        var mock = new Mock<IOrderReadRepository>();
        mock.Setup(r => r.GetOrderReportRows(from, to))
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-06-A: serie con 2 dias
    [Fact]
    public async Task Handle_ConOrdenesEnDosDias_RetornaSerieTemporal()
    {
        var dia1 = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var dia2 = new DateTime(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc);

        var rows = new List<OrderReportRow>
        {
            new() { OrderStatus = nameof(OrderStatus.Approved), Total = 1000, BuyerEmail = "a@test.com", CreatedAt = dia1 },
            new() { OrderStatus = nameof(OrderStatus.Approved), Total = 1000, BuyerEmail = "b@test.com", CreatedAt = dia1 },
            new() { OrderStatus = nameof(OrderStatus.Approved), Total = 1000, BuyerEmail = "c@test.com", CreatedAt = dia1 },
            new() { OrderStatus = nameof(OrderStatus.Pending),  Total = 750,  BuyerEmail = "d@test.com", CreatedAt = dia2 },
            new() { OrderStatus = nameof(OrderStatus.Pending),  Total = 750,  BuyerEmail = "e@test.com", CreatedAt = dia2 },
        };

        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc);

        var repoMock = SetupMock(rows, from, to);
        var handler = new GetOrdersByDateReportHandler(repoMock.Object);

        var query = new GetOrdersByDateReportQuery
        {
            From = from,
            To = to,
            Granularity = DateGranularity.Day
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var puntoDia1 = result.Value!.FirstOrDefault(p => p.Date.Date == dia1.Date);
        puntoDia1.Should().NotBeNull();
        puntoDia1!.OrderCount.Should().Be(3);
        puntoDia1.Revenue.Should().Be(3000);

        var puntoDia2 = result.Value.FirstOrDefault(p => p.Date.Date == dia2.Date);
        puntoDia2.Should().NotBeNull();
        puntoDia2!.OrderCount.Should().Be(2);
        puntoDia2.Revenue.Should().Be(1500);
    }

    // RFC-06-B: rango sin ordenes
    [Fact]
    public async Task Handle_SinOrdenesEnRango_RetornaListaVacia()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var repoMock = SetupMock([], from, to);
        var handler = new GetOrdersByDateReportHandler(repoMock.Object);

        var query = new GetOrdersByDateReportQuery
        {
            From = from,
            To = to,
            Granularity = DateGranularity.Day
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
