using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.OrderAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetTopBuyersReportHandlerTests
{
    private static Mock<IOrderReadRepository> SetupMock(List<OrderReportRow> rows)
    {
        var mock = new Mock<IOrderReadRepository>();
        mock.Setup(r => r.GetOrderReportRows(null, null))
            .Returns(rows.BuildMock());
        return mock;
    }

    private static List<OrderReportRow> GenerarCompradores(int cantidad, long revenueBase = 1000)
    {
        var rows = new List<OrderReportRow>();
        for (var i = 1; i <= cantidad; i++)
        {
            rows.Add(new OrderReportRow
            {
                OrderStatus = nameof(OrderStatus.PaymentRecieved),
                Total = revenueBase * i, // revenue distinto para que el orden sea determinista
                BuyerEmail = $"buyer{i:D2}@test.com",
                CreatedAt = DateTime.UtcNow
            });
        }
        return rows;
    }

    // RFC-08-A: limit por defecto (10 registros)
    [Fact]
    public async Task Handle_ConMasDeDiezCompradores_RetornaDiezRegistros()
    {
        var rows = GenerarCompradores(15);
        var repoMock = SetupMock(rows);
        var handler = new GetTopBuyersReportHandler(repoMock.Object);

        var result = await handler.Handle(
            new GetTopBuyersReportQuery { Limit = 10 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(10);
    }

    // RFC-08-B: limit explicito
    [Fact]
    public async Task Handle_ConLimitExplicito_RetornaLaCantidadSolicitada()
    {
        var rows = GenerarCompradores(15);
        var repoMock = SetupMock(rows);
        var handler = new GetTopBuyersReportHandler(repoMock.Object);

        var result = await handler.Handle(
            new GetTopBuyersReportQuery { Limit = 5 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }

    // Los compradores deben estar ordenados por revenue descendente
    [Fact]
    public async Task Handle_ResultadosOrdenadosPorRevenueDescendente()
    {
        var rows = new List<OrderReportRow>
        {
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 500,  BuyerEmail = "bajo@test.com",  CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 3000, BuyerEmail = "alto@test.com",  CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1500, BuyerEmail = "medio@test.com", CreatedAt = DateTime.UtcNow },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetTopBuyersReportHandler(repoMock.Object);

        var result = await handler.Handle(
            new GetTopBuyersReportQuery { Limit = 10 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value![0].BuyerEmail.Should().Be("alto@test.com");
        result.Value[1].BuyerEmail.Should().Be("medio@test.com");
        result.Value[2].BuyerEmail.Should().Be("bajo@test.com");
    }

    // Agrupacion por BuyerEmail — multiples ordenes del mismo comprador
    [Fact]
    public async Task Handle_CompradoresConMultiplesOrdenes_AgrupaPorEmail()
    {
        var rows = new List<OrderReportRow>
        {
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 1000, BuyerEmail = "repeat@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 2000, BuyerEmail = "repeat@test.com", CreatedAt = DateTime.UtcNow },
            new() { OrderStatus = nameof(OrderStatus.PaymentRecieved), Total = 500,  BuyerEmail = "otro@test.com",   CreatedAt = DateTime.UtcNow },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetTopBuyersReportHandler(repoMock.Object);

        var result = await handler.Handle(
            new GetTopBuyersReportQuery { Limit = 10 },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var compradorRepetido = result.Value!.FirstOrDefault(b => b.BuyerEmail == "repeat@test.com");
        compradorRepetido.Should().NotBeNull();
        compradorRepetido!.OrderCount.Should().Be(2);
        compradorRepetido.TotalRevenue.Should().Be(3000);
    }
}
