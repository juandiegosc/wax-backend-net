using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.ProductAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetCustomProductsFunnelReportHandlerTests
{
    private static Mock<ICustomProductReadRepository> SetupMock(List<CustomProductReportRow> rows)
    {
        var mock = new Mock<ICustomProductReadRepository>();
        mock.Setup(r => r.GetCustomProductReportRows())
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-12-A: funnel basico — estados con datos
    [Fact]
    public async Task Handle_ConProductosEnVariosEstados_RetornaConteoPorEstado()
    {
        var rows = new List<CustomProductReportRow>
        {
            new() { Status = nameof(CustomProductStatus.PendingQuotation),     AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.PendingQuotation),     AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.PendingQuotation),     AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.PendingQuotation),     AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.PendingQuotation),     AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.AwaitingAdminReview),  AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.AwaitingAdminReview),  AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.AwaitingAdminReview),  AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.Approved),             AgreedPrice = 1000 },
            new() { Status = nameof(CustomProductStatus.Approved),             AgreedPrice = 2000 },
            new() { Status = nameof(CustomProductStatus.Rejected),             AgreedPrice = null },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetCustomProductsFunnelReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetCustomProductsFunnelReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var lista = resultado.Value!;

        // Todos los estados del enum deben estar presentes
        lista.Should().HaveCount(6);

        lista.Should().Contain(f => f.Status == nameof(CustomProductStatus.PendingQuotation)    && f.Count == 5);
        lista.Should().Contain(f => f.Status == nameof(CustomProductStatus.AwaitingAdminReview) && f.Count == 3);
        lista.Should().Contain(f => f.Status == nameof(CustomProductStatus.Approved)            && f.Count == 2);
        lista.Should().Contain(f => f.Status == nameof(CustomProductStatus.Rejected)            && f.Count == 1);
        // Estados con 0 registros deben aparecer igualmente
        lista.Should().Contain(f => f.Status == nameof(CustomProductStatus.AwaitingCustomerReview) && f.Count == 0);
        lista.Should().Contain(f => f.Status == nameof(CustomProductStatus.AddedToBasket)          && f.Count == 0);
    }

    // Sin custom products — todos los estados deben aparecer con conteo 0
    [Fact]
    public async Task Handle_SinCustomProducts_RetornaTodosLosEstadosConCeroCada()
    {
        var repoMock = SetupMock([]);
        var handler = new GetCustomProductsFunnelReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetCustomProductsFunnelReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var lista = resultado.Value!;

        lista.Should().HaveCount(6);
        lista.Should().AllSatisfy(f => f.Count.Should().Be(0));
    }

    // Solo un estado — los otros cinco deben tener Count 0
    [Fact]
    public async Task Handle_SoloUnEstado_RellenaCeroParaElResto()
    {
        var rows = new List<CustomProductReportRow>
        {
            new() { Status = nameof(CustomProductStatus.Rejected), AgreedPrice = null },
            new() { Status = nameof(CustomProductStatus.Rejected), AgreedPrice = null },
        };

        var repoMock = SetupMock(rows);
        var handler = new GetCustomProductsFunnelReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetCustomProductsFunnelReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var lista = resultado.Value!;

        lista.Should().HaveCount(6);
        lista.Should().Contain(f => f.Status == nameof(CustomProductStatus.Rejected) && f.Count == 2);
        lista.Where(f => f.Status != nameof(CustomProductStatus.Rejected))
             .Should().AllSatisfy(f => f.Count.Should().Be(0));
    }
}
