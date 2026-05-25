using API.Controllers;
using Application.Core.Validations;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UnitTests.Helpers;

namespace UnitTests.API.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IMediator> _mediator;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        var store = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        (_mediator, _controller) = ControllerTestFactory.Create(new ReportsController(userManager.Object));
    }

    private void SetupQuery<TQuery, TResult>(TResult value)
        where TQuery : IRequest<Result<TResult>>
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<TQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TResult>.Success(value));
    }

    [Fact]
    public async Task GetOrdersSummary_DelegaYRetornaOk()
    {
        SetupQuery<GetOrdersSummaryReportQuery, OrdersSummaryReportDto>(new OrdersSummaryReportDto());

        var result = await _controller.GetOrdersSummary();

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetOrdersSummaryReportQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrdersByDate_DelegaConParametrosYRetornaOk()
    {
        SetupQuery<GetOrdersByDateReportQuery, List<OrdersByDateReportDto>>([]);

        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);
        var result = await _controller.GetOrdersByDate(from, to);

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<GetOrdersByDateReportQuery>(q => q.From == from && q.To == to),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrdersByStatus_DelegaYRetornaOk()
    {
        SetupQuery<GetOrdersByStatusReportQuery, List<OrdersByStatusReportDto>>([]);

        var result = await _controller.GetOrdersByStatus();

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetOrdersByStatusReportQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTopBuyers_DelegaConLimitYRetornaOk()
    {
        SetupQuery<GetTopBuyersReportQuery, List<TopBuyerReportDto>>([]);

        var result = await _controller.GetTopBuyers(5);

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(It.Is<GetTopBuyersReportQuery>(q => q.Limit == 5), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentsSummary_DelegaYRetornaOk()
    {
        SetupQuery<GetPaymentsSummaryReportQuery, PaymentsSummaryReportDto>(new PaymentsSummaryReportDto());

        var result = await _controller.GetPaymentsSummary();

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetPaymentsSummaryReportQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductsStock_DelegaConThresholdYRetornaOk()
    {
        SetupQuery<GetProductsStockReportQuery, ProductsStockReportDto>(new ProductsStockReportDto());

        var result = await _controller.GetProductsStock(15);

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(It.Is<GetProductsStockReportQuery>(q => q.Threshold == 15), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProductsByTypeBrand_DelegaYRetornaOk()
    {
        SetupQuery<GetProductsByTypeBrandReportQuery, List<ProductsByTypeBrandReportDto>>([]);

        var result = await _controller.GetProductsByTypeBrand();

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetProductsByTypeBrandReportQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCustomProductsFunnel_DelegaYRetornaOk()
    {
        SetupQuery<GetCustomProductsFunnelReportQuery, List<CustomProductsFunnelReportDto>>([]);

        var result = await _controller.GetCustomProductsFunnel();

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetCustomProductsFunnelReportQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCustomProductsPriceStats_DelegaYRetornaOk()
    {
        SetupQuery<GetCustomProductsPriceStatsReportQuery, CustomProductsPriceStatsReportDto>(
            new CustomProductsPriceStatsReportDto());

        var result = await _controller.GetCustomProductsPriceStats();

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetCustomProductsPriceStatsReportQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSupportSummary_DelegaYRetornaOk()
    {
        SetupQuery<GetSupportSummaryReportQuery, SupportSummaryReportDto>(new SupportSummaryReportDto());

        var result = await _controller.GetSupportSummary();

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetSupportSummaryReportQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSupportByDate_DelegaConParametrosYRetornaOk()
    {
        SetupQuery<GetSupportByDateReportQuery, List<SupportByDateReportDto>>([]);

        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);
        var result = await _controller.GetSupportByDate(from, to);

        result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(
            m => m.Send(
                It.Is<GetSupportByDateReportQuery>(q => q.From == from && q.To == to),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
