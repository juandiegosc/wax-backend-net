using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.Queries;
using Application.SupportAssist.DTOs;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetSupportByDateReportHandlerTests
{
    private static Mock<ISupportTicketReadRepository> SetupMock(List<SupportTicketDto> rows)
    {
        var mock = new Mock<ISupportTicketReadRepository>();
        mock.Setup(r => r.GetSupportTickets())
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-15-A: serie de 2 dias con conteos correctos por dia
    [Fact]
    public async Task Handle_ConTicketsEnDosDias_RetornaConteoPorDia()
    {
        var rows = new List<SupportTicketDto>
        {
            new() { Id = "1", CreatedAt = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc) },
            new() { Id = "2", CreatedAt = new DateTime(2026, 5, 1, 14, 0, 0, DateTimeKind.Utc) },
            new() { Id = "3", CreatedAt = new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc) }
        };

        var handler = new GetSupportByDateReportHandler(SetupMock(rows).Object);

        var resultado = await handler.Handle(
            new GetSupportByDateReportQuery
            {
                From = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc)
            },
            CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var lista = resultado.Value!;

        lista.Should().HaveCount(2);
        lista[0].Date.Should().Be(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        lista[0].TicketCount.Should().Be(2);
        lista[1].Date.Should().Be(new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));
        lista[1].TicketCount.Should().Be(1);
    }

    // Rango sin tickets — lista vacia
    [Fact]
    public async Task Handle_SinTicketsEnRango_RetornaListaVacia()
    {
        var rows = new List<SupportTicketDto>
        {
            new() { Id = "1", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        var handler = new GetSupportByDateReportHandler(SetupMock(rows).Object);

        var resultado = await handler.Handle(
            new GetSupportByDateReportQuery
            {
                From = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc)
            },
            CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Should().BeEmpty();
    }
}
