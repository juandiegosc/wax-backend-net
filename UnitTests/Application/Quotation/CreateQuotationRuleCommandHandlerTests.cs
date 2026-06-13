using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Quotation.Commands;
using Application.Quotation.DTOs;
using Domain.ProductAggregate;

namespace UnitTests.Application.Quotation;

public class CreateQuotationRuleCommandHandlerTests
{
    private readonly Mock<IQuotationRuleRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IQuotationRulesCache> _cache = new();
    private readonly CreateQuotationRuleCommandHandler _handler;

    public CreateQuotationRuleCommandHandlerTests()
    {
        _handler = new CreateQuotationRuleCommandHandler(_repo.Object, _unitOfWork.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_RetornaExitoYLlamaInvalidateAsync()
    {
        _repo.Setup(r => r.ExistsByKeyAsync("NEW_KEY", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateQuotationRuleCommand
        {
            Dto = new CreateQuotationRuleDto { Key = "NEW_KEY", Value = 10m, Description = "desc" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Key.Should().Be("NEW_KEY");
        _repo.Verify(r => r.AddAsync(It.IsAny<QuotationRule>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClaveDuplicada_RetornaFalla400YNoLlamaAddNiInvalidate()
    {
        _repo.Setup(r => r.ExistsByKeyAsync("DUP_KEY", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateQuotationRuleCommand
        {
            Dto = new CreateQuotationRuleDto { Key = "DUP_KEY", Value = 5m }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(400);
        _repo.Verify(r => r.AddAsync(It.IsAny<QuotationRule>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
