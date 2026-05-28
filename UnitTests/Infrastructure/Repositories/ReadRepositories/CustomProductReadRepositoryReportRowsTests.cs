using Infrastructure.Repositories.ReadRepositories;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.ReadModels;

namespace UnitTests.Infrastructure.Repositories.ReadRepositories;

public class CustomProductReadRepositoryReportRowsTests
{
    private static ReadDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReadDbContext(options);
    }

    private static CustomProductReadModel CreateModel(string status, long? agreedPrice)
        => new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Custom",
            Description = "desc",
            Price = 0,
            PictureUrl = "url",
            TaskId = "task",
            GlbUrl = "glb",
            OwnerUserId = "owner",
            Status = status,
            DesignType = "ring",
            DesignMaterial = "gold",
            DesignColor = "yellow",
            DesignShape = "round",
            DesignDimensions = "1x1",
            AgreedPrice = agreedPrice,
            CreatedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task GetCustomProductReportRows_RetornaProyeccionEscalar()
    {
        using var context = CreateInMemoryContext();
        context.CustomProducts.Add(CreateModel("Approved", 5000));
        context.CustomProducts.Add(CreateModel("PendingQuotation", null));
        await context.SaveChangesAsync();

        var repository = new CustomProductReadRepository(context);

        var result = await repository.GetCustomProductReportRows().ToListAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Status == "Approved" && r.AgreedPrice == 5000);
        result.Should().Contain(r => r.Status == "PendingQuotation" && r.AgreedPrice == null);
    }

    [Fact]
    public async Task GetCustomProductReportRows_PermiteAgregacionPorEstado()
    {
        using var context = CreateInMemoryContext();
        context.CustomProducts.Add(CreateModel("Approved", 1000));
        context.CustomProducts.Add(CreateModel("Approved", 3000));
        context.CustomProducts.Add(CreateModel("Rejected", null));
        await context.SaveChangesAsync();

        var repository = new CustomProductReadRepository(context);

        var aprobados = await repository.GetCustomProductReportRows()
            .Where(r => r.Status == "Approved" && r.AgreedPrice != null)
            .ToListAsync();

        aprobados.Should().HaveCount(2);
        aprobados.Select(r => r.AgreedPrice!.Value).Average().Should().Be(2000);
    }
}
