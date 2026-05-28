using Application.Interfaces.Repositories.ReadRepositories;
using Application.Product.DTOs;
using Application.Reports.Queries;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetProductsStockReportHandlerTests
{
    private static Mock<IProductReadRepository> SetupMock(List<ProductDto> productos)
    {
        var mock = new Mock<IProductReadRepository>();
        mock.Setup(r => r.GetQueryable())
            .Returns(productos.BuildMock());
        return mock;
    }

    private static ProductDto MakeProduct(int qty) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = "Producto",
        Description = "Desc",
        Price = 1000,
        PictureUrl = "http://img.test",
        Type = "Tipo",
        Brand = "Marca",
        QuantityInStock = qty
    };

    // RFC-10-A: threshold por defecto (10)
    [Fact]
    public async Task Handle_ConProductosMixtos_RetornaConteosCorrectosConThresholdPorDefecto()
    {
        var productos = new List<ProductDto>
        {
            MakeProduct(0),  // agotado
            MakeProduct(0),  // agotado
            MakeProduct(1),  // bajo stock (<= 10)
            MakeProduct(5),  // bajo stock
            MakeProduct(10), // bajo stock (== threshold)
            MakeProduct(11), // normal
            MakeProduct(50), // normal
            MakeProduct(100),// normal
            MakeProduct(200),// normal
            MakeProduct(999) // normal
        };

        var repoMock = SetupMock(productos);
        var handler = new GetProductsStockReportHandler(repoMock.Object);

        var resultado = await handler.Handle(
            new GetProductsStockReportQuery { Threshold = 10 }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.TotalProducts.Should().Be(10);
        resultado.Value.OutOfStock.Should().Be(2);
        resultado.Value.LowStock.Should().Be(3);
        resultado.Value.InStock.Should().Be(5);
    }

    // RFC-10-B: threshold personalizado (5)
    [Fact]
    public async Task Handle_ConThresholdPersonalizado_RetornaConteosSegunUmbral()
    {
        var productos = new List<ProductDto>
        {
            MakeProduct(0), // agotado
            MakeProduct(1), // bajo (<= 5)
            MakeProduct(3), // bajo
            MakeProduct(5), // bajo (== threshold)
            MakeProduct(6), // normal
            MakeProduct(8), // normal
            MakeProduct(20),// normal
            MakeProduct(50),// normal
            MakeProduct(100)// normal
        };

        var repoMock = SetupMock(productos);
        var handler = new GetProductsStockReportHandler(repoMock.Object);

        var resultado = await handler.Handle(
            new GetProductsStockReportQuery { Threshold = 5 }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.TotalProducts.Should().Be(9);
        resultado.Value.OutOfStock.Should().Be(1);
        resultado.Value.LowStock.Should().Be(3);
        resultado.Value.InStock.Should().Be(5);
    }

    // Sin productos
    [Fact]
    public async Task Handle_SinProductos_RetornaTodosConteosEnCero()
    {
        var repoMock = SetupMock([]);
        var handler = new GetProductsStockReportHandler(repoMock.Object);

        var resultado = await handler.Handle(
            new GetProductsStockReportQuery { Threshold = 10 }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.TotalProducts.Should().Be(0);
        resultado.Value.OutOfStock.Should().Be(0);
        resultado.Value.LowStock.Should().Be(0);
        resultado.Value.InStock.Should().Be(0);
    }
}
