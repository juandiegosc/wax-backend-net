using Application.Interfaces.Repositories.WriteRepositories;
using Application.Quotation.Queries;
using Domain.ProductAggregate;

namespace UnitTests.Application.Quotation;

public class GetQuotationRulesQueryHandlerTests
{
    private readonly Mock<IQuotationRuleRepository> _repo = new();
    private readonly GetQuotationRulesQueryHandler _handler;

    public GetQuotationRulesQueryHandlerTests()
    {
        _handler = new GetQuotationRulesQueryHandler(_repo.Object);
    }

    private static QuotationRule BuildRule(string key, bool isActive)
    {
        var rule = new QuotationRule { Key = key, Value = 1m };
        if (!isActive) rule.Deactivate();
        return rule;
    }

    [Fact]
    public async Task Handle_ActiveOnlyNull_RetornaTodas()
    {
        var rules = new List<QuotationRule>
        {
            BuildRule("KEY_1", isActive: true),
            BuildRule("KEY_2", isActive: false)
        };

        _repo.Setup(r => r.ListAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.AsReadOnly());

        var result = await _handler.Handle(new GetQuotationRulesQuery { ActiveOnly = null }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ActiveOnlyTrue_RetornaSoloActivas()
    {
        var rules = new List<QuotationRule>
        {
            BuildRule("KEY_1", isActive: true)
        };

        _repo.Setup(r => r.ListAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.AsReadOnly());

        var result = await _handler.Handle(new GetQuotationRulesQuery { ActiveOnly = true }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ListaVacia_RetornaExitoConListaVacia()
    {
        _repo.Setup(r => r.ListAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QuotationRule>().AsReadOnly());

        var result = await _handler.Handle(new GetQuotationRulesQuery { ActiveOnly = null }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
