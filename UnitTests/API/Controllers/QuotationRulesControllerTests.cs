using API.Controllers;
using Application.Core.Validations;
using Application.Quotation.Commands;
using Application.Quotation.DTOs;
using Application.Quotation.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UnitTests.Helpers;

namespace UnitTests.API.Controllers;

public class QuotationRulesControllerTests
{
    private readonly Mock<IMediator> _mediator;
    private readonly QuotationRulesController _controller;

    public QuotationRulesControllerTests()
    {
        (_mediator, _controller) = ControllerTestFactory.Create(new QuotationRulesController());
    }

    private static QuotationRuleDto SampleDto(string id = "rule-1", string key = "BASE_COST", bool isActive = true, bool isDefault = false)
        => new(id, key, 5000m, "desc", isActive, isDefault);

    [Fact]
    public async Task CreateQuotationRule_DelegaConDtoYRetornaOk()
    {
        var dto = new CreateQuotationRuleDto { Key = "NEW_KEY", Value = 10m, Description = "x" };
        _mediator
            .Setup(m => m.Send(It.IsAny<CreateQuotationRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QuotationRuleDto>.Success(SampleDto(key: "NEW_KEY")));

        var result = await _controller.CreateQuotationRule(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<CreateQuotationRuleCommand>(c => c.Dto == dto),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetQuotationRules_SinFiltro_DelegaConActiveOnlyNull()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<GetQuotationRulesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<QuotationRuleDto>>.Success([SampleDto()]));

        var result = await _controller.GetQuotationRules(activeOnly: null);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<GetQuotationRulesQuery>(q => q.ActiveOnly == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetQuotationRules_ConActiveOnlyTrue_DelegaConFiltro()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<GetQuotationRulesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<QuotationRuleDto>>.Success([]));

        var result = await _controller.GetQuotationRules(activeOnly: true);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<GetQuotationRulesQuery>(q => q.ActiveOnly == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetQuotationRuleById_DelegaConIdYRetornaOk()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<GetQuotationRuleByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QuotationRuleDto>.Success(SampleDto(id: "rule-42")));

        var result = await _controller.GetQuotationRuleById("rule-42");

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<GetQuotationRuleByIdQuery>(q => q.Id == "rule-42"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateQuotationRule_DelegaConIdYDtoYRetornaOk()
    {
        var dto = new UpdateQuotationRuleDto { Value = 99m, Description = "upd", IsActive = false };
        _mediator
            .Setup(m => m.Send(It.IsAny<UpdateQuotationRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QuotationRuleDto>.Success(SampleDto(id: "rule-7", isActive: false)));

        var result = await _controller.UpdateQuotationRule("rule-7", dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<UpdateQuotationRuleCommand>(c => c.Id == "rule-7" && c.Dto == dto),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteQuotationRule_DelegaConIdYRetornaOk()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<DeleteQuotationRuleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteQuotationRule("rule-3");

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<DeleteQuotationRuleCommand>(c => c.Id == "rule-3"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
