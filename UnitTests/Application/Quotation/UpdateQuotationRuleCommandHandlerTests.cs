using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Quotation.Commands;
using Application.Quotation.DTOs;
using Domain.ProductAggregate;

namespace UnitTests.Application.Quotation;

public class UpdateQuotationRuleCommandHandlerTests
{
    private readonly Mock<IQuotationRuleRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IQuotationRulesCache> _cache = new();
    private readonly UpdateQuotationRuleCommandHandler _handler;

    public UpdateQuotationRuleCommandHandlerTests()
    {
        _handler = new UpdateQuotationRuleCommandHandler(_repo.Object, _unitOfWork.Object, _cache.Object);
    }

    private static QuotationRule BuildRule(bool isDefault = false)
    {
        var rule = new QuotationRule { Key = "BASE_COST", Value = 5000m };
        if (isDefault) rule.MarkAsDefault();
        return rule;
    }

    [Fact]
    public async Task Handle_HappyPathReglaNormal_RetornaExitoYInvalidaCache()
    {
        var rule = BuildRule(isDefault: false);
        _repo.Setup(r => r.GetTrackedByIdAsync("id-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateQuotationRuleCommand
        {
            Id = "id-1",
            Dto = new UpdateQuotationRuleDto { Value = 9999m, Description = "nueva", IsActive = true }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HappyPathReglaDefault_RetornaExitoActualizaPermitido()
    {
        var rule = BuildRule(isDefault: true);
        _repo.Setup(r => r.GetTrackedByIdAsync("id-default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateQuotationRuleCommand
        {
            Id = "id-default",
            Dto = new UpdateQuotationRuleDto { Value = 6000m, Description = "actualizada", IsActive = true }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReglaNoExiste_RetornaFalla404SinInvalidar()
    {
        _repo.Setup(r => r.GetTrackedByIdAsync("no-existe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuotationRule?)null);

        var command = new UpdateQuotationRuleCommand
        {
            Id = "no-existe",
            Dto = new UpdateQuotationRuleDto { Value = 1m, IsActive = true }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(404);
        _cache.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
