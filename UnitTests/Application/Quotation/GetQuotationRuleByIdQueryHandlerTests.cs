using Application.Interfaces.Repositories.WriteRepositories;
using Application.Quotation.Queries;
using Domain.ProductAggregate;

namespace UnitTests.Application.Quotation;

public class GetQuotationRuleByIdQueryHandlerTests
{
    private readonly Mock<IQuotationRuleRepository> _repo = new();
    private readonly GetQuotationRuleByIdQueryHandler _handler;

    public GetQuotationRuleByIdQueryHandlerTests()
    {
        _handler = new GetQuotationRuleByIdQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ReglaEncontrada_RetornaDto()
    {
        var rule = new QuotationRule { Key = "BASE_COST", Value = 5000m };
        _repo.Setup(r => r.GetByIdAsync("id-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var result = await _handler.Handle(new GetQuotationRuleByIdQuery { Id = "id-1" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Key.Should().Be("BASE_COST");
    }

    [Fact]
    public async Task Handle_ReglaNoEncontrada_RetornaFalla404()
    {
        _repo.Setup(r => r.GetByIdAsync("no-existe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuotationRule?)null);

        var result = await _handler.Handle(new GetQuotationRuleByIdQuery { Id = "no-existe" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(404);
    }
}
