using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.ProductAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetCustomProductsPriceStatsReportHandlerTests
{
    private static Mock<ICustomProductReadRepository> SetupMock(List<CustomProductReportRow> rows)
    {
        var mock = new Mock<ICustomProductReadRepository>();
        mock.Setup(r => r.GetCustomProductReportRows())
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-13-A: estadisticas con datos
    [Fact]
    public async Task Handle_ConTresAprobadosConPrecio_RetornaMinMaxPromedio()
    {
        var rows = new List<CustomProductReportRow>
        {
            new() { Status = nameof(CustomProductStatus.Approved), AgreedPrice = 1000 },
            new() { Status = nameof(CustomProductStatus.Approved), AgreedPrice = 2000 },
            new() { Status = nameof(CustomProductStatus.Approved), AgreedPrice = 3000 },
            // Estado distinto — no debe afectar las estadisticas
            new() { Status = nameof(CustomProductStatus.Rejected), AgreedPrice = 500 },
            // Approved pero sin precio — no debe contarse
            new() { Status = nameof(CustomProductStatus.Approved), AgreedPrice = null },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetCustomProductsPriceStatsReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetCustomProductsPriceStatsReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var dto = resultado.Value!;

        dto.ApprovedCount.Should().Be(3);
        dto.MinPrice.Should().Be(1000);
        dto.MaxPrice.Should().Be(3000);
        dto.AveragePrice.Should().Be(2000);
    }

    // RFC-13-B: sin custom products aprobados — NO debe lanzar excepcion 500
    [Fact]
    public async Task Handle_SinAprobados_RetornaConteoEnCeroYStatsNull()
    {
        var rows = new List<CustomProductReportRow>
        {
            new() { Status = nameof(CustomProductStatus.PendingQuotation), AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.Rejected),         AgreedPrice = null },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetCustomProductsPriceStatsReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetCustomProductsPriceStatsReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var dto = resultado.Value!;

        dto.ApprovedCount.Should().Be(0);
        dto.MinPrice.Should().BeNull();
        dto.MaxPrice.Should().BeNull();
        dto.AveragePrice.Should().BeNull();
    }

    // Sin datos en absoluto — misma guarda contra division por cero / agregacion vacia
    [Fact]
    public async Task Handle_SinDatos_RetornaConteoEnCeroYStatsNull()
    {
        var repoMock = SetupMock([]);
        var handler = new GetCustomProductsPriceStatsReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetCustomProductsPriceStatsReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var dto = resultado.Value!;

        dto.ApprovedCount.Should().Be(0);
        dto.MinPrice.Should().BeNull();
        dto.MaxPrice.Should().BeNull();
        dto.AveragePrice.Should().BeNull();
    }

    // Solo Approved con AgreedPrice nulo — ApprovedCount = 0 (filtra AgreedPrice != null)
    [Fact]
    public async Task Handle_AprobadosSinPrecio_RetornaConteoEnCeroYStatsNull()
    {
        var rows = new List<CustomProductReportRow>
        {
            new() { Status = nameof(CustomProductStatus.Approved), AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.Approved), AgreedPrice = null },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetCustomProductsPriceStatsReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetCustomProductsPriceStatsReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var dto = resultado.Value!;

        dto.ApprovedCount.Should().Be(0);
        dto.MinPrice.Should().BeNull();
        dto.MaxPrice.Should().BeNull();
        dto.AveragePrice.Should().BeNull();
    }
}
