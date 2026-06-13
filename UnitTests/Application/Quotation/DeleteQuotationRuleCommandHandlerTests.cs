using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Quotation.Commands;
using Domain.ProductAggregate;

namespace UnitTests.Application.Quotation;

public class DeleteQuotationRuleCommandHandlerTests
{
    private readonly Mock<IQuotationRuleRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IQuotationRulesCache> _cache = new();
    private readonly DeleteQuotationRuleCommandHandler _handler;

    public DeleteQuotationRuleCommandHandlerTests()
    {
        _handler = new DeleteQuotationRuleCommandHandler(_repo.Object, _unitOfWork.Object, _cache.Object);
    }

    private static QuotationRule BuildRule(bool isDefault = false)
    {
        var rule = new QuotationRule { Key = "SOME_KEY", Value = 10m };
        if (isDefault) rule.MarkAsDefault();
        return rule;
    }

    [Fact]
    public async Task Handle_ReglaNormal_DesactivaYInvalidaCache()
    {
        var rule = BuildRule(isDefault: false);
        _repo.Setup(r => r.GetTrackedByIdAsync("id-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DeleteQuotationRuleCommand { Id = "id-1" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        rule.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReglaDefault_RetornaFalla400SinCompleteNiInvalidate()
    {
        var rule = BuildRule(isDefault: true);
        _repo.Setup(r => r.GetTrackedByIdAsync("id-default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var command = new DeleteQuotationRuleCommand { Id = "id-default" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(400);
        _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReglaNoExiste_RetornaFalla404()
    {
        _repo.Setup(r => r.GetTrackedByIdAsync("no-existe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuotationRule?)null);

        var command = new DeleteQuotationRuleCommand { Id = "no-existe" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(404);
    }
}
