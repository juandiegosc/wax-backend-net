using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Application.SupportAssist.DTOs;
using Domain.SupportAssistAggregate;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetSupportSummaryReportHandlerTests
{
    private static Mock<ISupportTicketReadRepository> SetupMock(List<SupportTicketDto> rows)
    {
        var mock = new Mock<ISupportTicketReadRepository>();
        mock.Setup(r => r.GetSupportTickets())
            .Returns(rows.BuildMock());
        return mock;
    }

    // RFC-14-A: resumen mixto — conteos por status y por categoria
    [Fact]
    public async Task Handle_ConTicketsMixtos_RetornaConteosCorrectosDeStatusYCategoria()
    {
        var rows = new List<SupportTicketDto>
        {
            new() { Status = nameof(TicketStatus.Open),       Category = nameof(TicketCategory.OrderIssue)   },
            new() { Status = nameof(TicketStatus.Open),       Category = nameof(TicketCategory.OrderIssue)   },
            new() { Status = nameof(TicketStatus.Open),       Category = nameof(TicketCategory.Other)        },
            new() { Status = nameof(TicketStatus.Open),       Category = nameof(TicketCategory.Other)        },
            new() { Status = nameof(TicketStatus.Open),       Category = nameof(TicketCategory.Other)        },
            new() { Status = nameof(TicketStatus.InProgress), Category = nameof(TicketCategory.PaymentIssue) },
            new() { Status = nameof(TicketStatus.InProgress), Category = nameof(TicketCategory.PaymentIssue) },
            new() { Status = nameof(TicketStatus.InProgress), Category = nameof(TicketCategory.PaymentIssue) },
            new() { Status = nameof(TicketStatus.Closed),     Category = nameof(TicketCategory.ProductIssue) },
            new() { Status = nameof(TicketStatus.Closed),     Category = nameof(TicketCategory.ProductIssue) },
        };

        var mock = SetupMock(rows);
        var handler = new GetSupportSummaryReportHandler(mock.Object);

        var resultado = await handler.Handle(new GetSupportSummaryReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var dto = resultado.Value!;

        dto.Total.Should().Be(10);
        dto.Open.Should().Be(5);
        dto.InProgress.Should().Be(3);
        dto.Closed.Should().Be(2);

        dto.ByCategory.Should().ContainKey(nameof(TicketCategory.OrderIssue)).WhoseValue.Should().Be(2);
        dto.ByCategory.Should().ContainKey(nameof(TicketCategory.PaymentIssue)).WhoseValue.Should().Be(3);
        dto.ByCategory.Should().ContainKey(nameof(TicketCategory.ProductIssue)).WhoseValue.Should().Be(2);
        dto.ByCategory.Should().ContainKey(nameof(TicketCategory.Other)).WhoseValue.Should().Be(3);
    }

    // Escenario: sin tickets — todos los conteos son cero
    [Fact]
    public async Task Handle_SinTickets_RetornaTodoEnCero()
    {
        var mock = SetupMock([]);
        var handler = new GetSupportSummaryReportHandler(mock.Object);

        var resultado = await handler.Handle(new GetSupportSummaryReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var dto = resultado.Value!;

        dto.Total.Should().Be(0);
        dto.Open.Should().Be(0);
        dto.InProgress.Should().Be(0);
        dto.Closed.Should().Be(0);
        dto.ByCategory.Values.Should().AllSatisfy(c => c.Should().Be(0));
    }

    // Escenario: solo tickets Open, sin otras categorias
    [Fact]
    public async Task Handle_SoloTicketsOpen_RetornaOpenCorrectoyRestoCero()
    {
        var rows = new List<SupportTicketDto>
        {
            new() { Status = nameof(TicketStatus.Open), Category = nameof(TicketCategory.Other) },
            new() { Status = nameof(TicketStatus.Open), Category = nameof(TicketCategory.Other) },
        };

        var mock = SetupMock(rows);
        var handler = new GetSupportSummaryReportHandler(mock.Object);

        var resultado = await handler.Handle(new GetSupportSummaryReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var dto = resultado.Value!;

        dto.Total.Should().Be(2);
        dto.Open.Should().Be(2);
        dto.InProgress.Should().Be(0);
        dto.Closed.Should().Be(0);
    }
}
