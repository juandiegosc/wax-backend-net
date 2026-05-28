using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.OrderAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetOrdersByStatusReportHandlerTests
{
    private static Mock<IOrderReadRepository> SetupMock(List<OrderReportRow> rows)
    {
        var mock = new Mock<IOrderReadRepository>();
        mock.Setup(r => r.GetOrderReportRows(null, null))
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-07-A: distribucion correcta con porcentaje
    [Fact]
    public async Task Handle_ConDistribucionMixta_RetornaConteoYPorcentajeCorrectos()
    {
        var rows = new List<OrderReportRow>
        {
            new() { OrderStatus = nameof(OrderStatus.Approved), Total = 1000, BuyerEmail = "a@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.Approved), Total = 1000, BuyerEmail = "b@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.Approved), Total = 1000, BuyerEmail = "c@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.Approved), Total = 1000, BuyerEmail = "d@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.Rejected), Total = 500,  BuyerEmail = "e@test.com", CreatedAt = DateTime.UtcNow },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetOrdersByStatusReportHandler(repoMock.Object);

        var result = await handler.Handle(new GetOrdersByStatusReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var aprobadas = result.Value!.FirstOrDefault(s => s.Status == nameof(OrderStatus.Approved));
        aprobadas.Should().NotBeNull();
        aprobadas!.Count.Should().Be(4);
        aprobadas.Percentage.Should().BeApproximately(80, 0.01);

        var rechazadas = result.Value.FirstOrDefault(s => s.Status == nameof(OrderStatus.Rejected));
        rechazadas.Should().NotBeNull();
        rechazadas!.Count.Should().Be(1);
        rechazadas.Percentage.Should().BeApproximately(20, 0.01);
    }

    // Sin ordenes: porcentaje 0% para todos los estados
    [Fact]
    public async Task Handle_SinOrdenes_RetornaPorcentajeCeroPorTodosLosEstados()
    {
        var repoMock = SetupMock([]);
        var handler = new GetOrdersByStatusReportHandler(repoMock.Object);

        var result = await handler.Handle(new GetOrdersByStatusReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(s =>
        {
            s.Count.Should().Be(0);
            s.Percentage.Should().Be(0);
        });
    }
}
