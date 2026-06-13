using Domain.ProductAggregate;
using Infrastructure.Repositories.WriteRepositories;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace UnitTests.Infrastructure.Repositories.WriteRepositories;

public class QuotationRuleRepositoryTests
{
    private static WriteDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WriteDbContext(options);
    }

    private static QuotationRule CreateRule(string key, decimal value = 10m, bool isActive = true, bool isDefault = false)
    {
        var rule = new QuotationRule { Key = key, Value = value, IsActive = isActive };
        if (isDefault) rule.MarkAsDefault();
        return rule;
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExiste_RetornaRegla()
    {
        using var context = CreateInMemoryContext();
        var rule = CreateRule("KEY_A");
        context.QuotationRules.Add(rule);
        await context.SaveChangesAsync();

        var repo = new QuotationRuleRepository(context);
        var result = await repo.GetByIdAsync(rule.Id);

        result.Should().NotBeNull();
        result!.Key.Should().Be("KEY_A");
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNull()
    {
        using var context = CreateInMemoryContext();
        var repo = new QuotationRuleRepository(context);

        var result = await repo.GetByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DevuelveEntidadNoTrackeada()
    {
        using var context = CreateInMemoryContext();
        var rule = CreateRule("KEY_NT");
        context.QuotationRules.Add(rule);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new QuotationRuleRepository(context);
        var fetched = await repo.GetByIdAsync(rule.Id);

        fetched.Should().NotBeNull();
        context.Entry(fetched!).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task GetTrackedByIdAsync_DevuelveEntidadTrackeada()
    {
        using var context = CreateInMemoryContext();
        var rule = CreateRule("KEY_T");
        context.QuotationRules.Add(rule);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new QuotationRuleRepository(context);
        var fetched = await repo.GetTrackedByIdAsync(rule.Id);

        fetched.Should().NotBeNull();
        context.Entry(fetched!).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task GetTrackedByIdAsync_CuandoNoExiste_RetornaNull()
    {
        using var context = CreateInMemoryContext();
        var repo = new QuotationRuleRepository(context);

        var result = await repo.GetTrackedByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByKeyAsync_CuandoExiste_RetornaRegla()
    {
        using var context = CreateInMemoryContext();
        context.QuotationRules.Add(CreateRule("BASE_COST", 5000m));
        await context.SaveChangesAsync();

        var repo = new QuotationRuleRepository(context);
        var result = await repo.GetByKeyAsync("BASE_COST");

        result.Should().NotBeNull();
        result!.Value.Should().Be(5000m);
    }

    [Fact]
    public async Task GetByKeyAsync_CuandoNoExiste_RetornaNull()
    {
        using var context = CreateInMemoryContext();
        var repo = new QuotationRuleRepository(context);

        var result = await repo.GetByKeyAsync("MISSING");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByKeyAsync_CuandoExiste_RetornaTrue()
    {
        using var context = CreateInMemoryContext();
        context.QuotationRules.Add(CreateRule("EXISTING"));
        await context.SaveChangesAsync();

        var repo = new QuotationRuleRepository(context);

        (await repo.ExistsByKeyAsync("EXISTING")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByKeyAsync_CuandoNoExiste_RetornaFalse()
    {
        using var context = CreateInMemoryContext();
        var repo = new QuotationRuleRepository(context);

        (await repo.ExistsByKeyAsync("MISSING")).Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_SinFiltro_RetornaTodas()
    {
        using var context = CreateInMemoryContext();
        context.QuotationRules.AddRange(
            CreateRule("A", isActive: true),
            CreateRule("B", isActive: false),
            CreateRule("C", isActive: true));
        await context.SaveChangesAsync();

        var repo = new QuotationRuleRepository(context);
        var result = await repo.ListAsync(activeOnly: null);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListAsync_ConActiveOnlyTrue_RetornaSoloActivas()
    {
        using var context = CreateInMemoryContext();
        context.QuotationRules.AddRange(
            CreateRule("A", isActive: true),
            CreateRule("B", isActive: false),
            CreateRule("C", isActive: true));
        await context.SaveChangesAsync();

        var repo = new QuotationRuleRepository(context);
        var result = await repo.ListAsync(activeOnly: true);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.IsActive);
    }

    [Fact]
    public async Task ListAsync_ConActiveOnlyFalse_RetornaSoloInactivas()
    {
        using var context = CreateInMemoryContext();
        context.QuotationRules.AddRange(
            CreateRule("A", isActive: true),
            CreateRule("B", isActive: false));
        await context.SaveChangesAsync();

        var repo = new QuotationRuleRepository(context);
        var result = await repo.ListAsync(activeOnly: false);

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("B");
    }

    [Fact]
    public async Task ListAsync_TablaVacia_RetornaListaVacia()
    {
        using var context = CreateInMemoryContext();
        var repo = new QuotationRuleRepository(context);

        var result = await repo.ListAsync(activeOnly: null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_AgregaEntidadAlContexto()
    {
        using var context = CreateInMemoryContext();
        var repo = new QuotationRuleRepository(context);
        var rule = CreateRule("NEW_RULE", 42m);

        await repo.AddAsync(rule);
        await context.SaveChangesAsync();

        var persisted = await context.QuotationRules.FindAsync(rule.Id);
        persisted.Should().NotBeNull();
        persisted!.Key.Should().Be("NEW_RULE");
        persisted.Value.Should().Be(42m);
    }
}
