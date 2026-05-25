using Application.Interfaces.Repositories.ReadRepositories;
using Application.Product.DTOs;
using Application.Reports.Queries;
using MockQueryable;

namespace UnitTests.Application.Reports.Queries;

public class GetProductsByTypeBrandReportHandlerTests
{
    private static Mock<IProductReadRepository> SetupMock(List<ProductDto> productos)
    {
        var mock = new Mock<IProductReadRepository>();
        mock.Setup(r => r.GetQueryable())
            .Returns(productos.BuildMock());
        return mock;
    }

    private static ProductDto MakeProduct(string type, string brand) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = "Producto",
        Description = "Desc",
        Price = 1000,
        PictureUrl = "http://img.test",
        Type = type,
        Brand = brand,
        QuantityInStock = 5
    };

    // RFC-11-A: distribucion basica con multiples combinaciones tipo/marca
    [Fact]
    public async Task Handle_ConProductosMixtos_RetornaDistribucionPorTipoYMarca()
    {
        var productos = new List<ProductDto>
        {
            MakeProduct("Taza",     "BrandA"),
            MakeProduct("Taza",     "BrandA"),
            MakeProduct("Taza",     "BrandA"),
            MakeProduct("Taza",     "BrandB"),
            MakeProduct("Taza",     "BrandB"),
            MakeProduct("Camiseta", "BrandA"),
            MakeProduct("Camiseta", "BrandA"),
            MakeProduct("Camiseta", "BrandA"),
            MakeProduct("Camiseta", "BrandA"),
            MakeProduct("Camiseta", "BrandA"),
        };

        var repoMock = SetupMock(productos);
        var handler = new GetProductsByTypeBrandReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetProductsByTypeBrandReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(3);

        var tazaBrandA = resultado.Value!.FirstOrDefault(x => x.Type == "Taza" && x.Brand == "BrandA");
        tazaBrandA.Should().NotBeNull();
        tazaBrandA!.Count.Should().Be(3);

        var tazaBrandB = resultado.Value.FirstOrDefault(x => x.Type == "Taza" && x.Brand == "BrandB");
        tazaBrandB.Should().NotBeNull();
        tazaBrandB!.Count.Should().Be(2);

        var camisetaBrandA = resultado.Value.FirstOrDefault(x => x.Type == "Camiseta" && x.Brand == "BrandA");
        camisetaBrandA.Should().NotBeNull();
        camisetaBrandA!.Count.Should().Be(5);
    }

    // Sin productos: lista vacia
    [Fact]
    public async Task Handle_SinProductos_RetornaListaVacia()
    {
        var repoMock = SetupMock([]);
        var handler = new GetProductsByTypeBrandReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetProductsByTypeBrandReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEmpty();
    }

    // Un solo grupo
    [Fact]
    public async Task Handle_ConUnSoloTipoYMarca_RetornaUnGrupo()
    {
        var productos = new List<ProductDto>
        {
            MakeProduct("Taza", "BrandA"),
            MakeProduct("Taza", "BrandA"),
        };

        var repoMock = SetupMock(productos);
        var handler = new GetProductsByTypeBrandReportHandler(repoMock.Object);

        var resultado = await handler.Handle(new GetProductsByTypeBrandReportQuery(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(1);
        resultado.Value![0].Count.Should().Be(2);
    }
}
