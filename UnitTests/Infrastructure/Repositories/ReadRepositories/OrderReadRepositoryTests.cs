using System.Text.Json;
using Application.Orders.DTOs;
using Infrastructure.Repositories.ReadRepositories;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.ReadModels;

namespace UnitTests.Infrastructure.Repositories.ReadRepositories;

public class OrderReadRepositoryTests
{
    private static ReadDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReadDbContext(options);
    }

    private static OrderReadModel CreateOrderReadModel(string? id = null, string? paymentIntentId = null, string? userId = null)
    {
        var orderItems = new List<OrderItemDto>
        {
            new()
            {
                ProductId = "prod-1",
                Name = "Product 1",
                Price = 100,
                Quantity = 2
            }
        };

        return new OrderReadModel
        {
            Id = id ?? Guid.NewGuid().ToString(),
            BuyerEmail = "buyer@example.com",
            OrderStatus = "Pending",
            Subtotal = 200,
            DeliveryFee = 10,
            Total = 210,
            BillingName = "John Doe",
            BillingLine1 = "123 Main St",
            BillingLine2 = "Apt 4",
            BillingCity = "Testville",
            BillingState = "TS",
            BillingPostalCode = "12345",
            BillingCountry = "US",
            PaymentLast4 = 4242,
            PaymentBrand = "Visa",
            PaymentExpMonth = 12,
            PaymentExpYear = 2025,
            PaymentIntentId = paymentIntentId ?? "pi_test_123",
            UserId = userId,
            OrderItems = JsonSerializer.Serialize(orderItems),
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderExists_ReturnsDto()
    {
        using var context = CreateInMemoryContext();
        var orderId = Guid.NewGuid().ToString();
        context.Orders.Add(CreateOrderReadModel(orderId));
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetOrderByIdAsync(orderId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.BuyerEmail.Should().Be("buyer@example.com");
    }

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repository = new OrderReadRepository(context);

        var result = await repository.GetOrderByIdAsync("non-existent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPaymentIntentIdAsync_WhenOrderExists_ReturnsDto()
    {
        using var context = CreateInMemoryContext();
        var paymentIntentId = "pi_specific_123";
        context.Orders.Add(CreateOrderReadModel(paymentIntentId: paymentIntentId));
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetByPaymentIntentIdAsync(paymentIntentId);

        result.Should().NotBeNull();
        result!.BuyerEmail.Should().Be("buyer@example.com");
    }

    [Fact]
    public async Task GetByPaymentIntentIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repository = new OrderReadRepository(context);

        var result = await repository.GetByPaymentIntentIdAsync("non-existent-pi");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetQueryable_ReturnsAllOrdersAsDtos()
    {
        using var context = CreateInMemoryContext();
        context.Orders.Add(CreateOrderReadModel());
        context.Orders.Add(CreateOrderReadModel());
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetQueryable().ToListAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderByIdAsync_MapsAllFieldsCorrectly()
    {
        using var context = CreateInMemoryContext();
        var orderId = Guid.NewGuid().ToString();
        context.Orders.Add(CreateOrderReadModel(orderId));
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetOrderByIdAsync(orderId);

        result.Should().NotBeNull();
        result!.OrderStatus.Should().Be("Pending");
        result.Subtotal.Should().Be(200);
        result.DeliveryFee.Should().Be(10);
        result.Total.Should().Be(210);
        result.BillingAddress.Should().NotBeNull();
        result.BillingAddress!.Name.Should().Be("John Doe");
        result.BillingAddress.Line1.Should().Be("123 Main St");
        result.PaymentSummary.Should().NotBeNull();
        result.PaymentSummary!.Last4.Should().Be(4242);
        result.PaymentSummary.Brand.Should().Be("Visa");
    }

    [Fact]
    public async Task GetOrderByIdAsync_DeserializesOrderItems()
    {
        using var context = CreateInMemoryContext();
        var orderId = Guid.NewGuid().ToString();
        context.Orders.Add(CreateOrderReadModel(orderId));
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetOrderByIdAsync(orderId);

        result.Should().NotBeNull();
        result!.OrderItems.Should().HaveCount(1);
        result.OrderItems[0].ProductId.Should().Be("prod-1");
        result.OrderItems[0].Name.Should().Be("Product 1");
        result.OrderItems[0].Price.Should().Be(100);
        result.OrderItems[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task GetQueryable_AllowsFiltering()
    {
        using var context = CreateInMemoryContext();
        var pendingOrder = CreateOrderReadModel();
        pendingOrder.OrderStatus = "Pending";
        var completedOrder = CreateOrderReadModel();
        completedOrder.OrderStatus = "Completed";
        context.Orders.Add(pendingOrder);
        context.Orders.Add(completedOrder);
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetQueryable()
            .Where(o => o.OrderStatus == "Pending")
            .ToListAsync();

        result.Should().HaveCount(1);
        result[0].OrderStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task GetQueryable_WithUserIdFilter_ReturnsOnlyMatchingOrders()
    {
        const string userId = "user-abc";
        using var context = CreateInMemoryContext();
        context.Orders.Add(CreateOrderReadModel(userId: userId));
        context.Orders.Add(CreateOrderReadModel(userId: userId));
        context.Orders.Add(CreateOrderReadModel(userId: "other-user"));
        context.Orders.Add(CreateOrderReadModel(userId: null));
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetQueryable(userId: userId).ToListAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.UserId == userId);
    }

    [Fact]
    public async Task GetQueryable_WithNullUserIdFilter_ReturnsAllOrders()
    {
        using var context = CreateInMemoryContext();
        context.Orders.Add(CreateOrderReadModel(userId: "user-1"));
        context.Orders.Add(CreateOrderReadModel(userId: "user-2"));
        context.Orders.Add(CreateOrderReadModel(userId: null));
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetQueryable().ToListAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetQueryable_WithStatusAndUserIdFilter_AppliesBothFilters()
    {
        const string userId = "user-xyz";
        using var context = CreateInMemoryContext();
        context.Orders.Add(CreateOrderReadModel(userId: userId));
        context.Orders.Add(CreateOrderReadModel(userId: userId));
        context.Orders.Add(CreateOrderReadModel(userId: "other-user"));
        await context.SaveChangesAsync();

        // Manually update status to allow combined filter test
        var orders = await context.Orders.ToListAsync();
        orders[0].OrderStatus = "Pending";
        orders[1].OrderStatus = "Completed";
        orders[2].OrderStatus = "Pending";
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetQueryable(statusFilter: "Pending", userId: userId).ToListAsync();

        result.Should().HaveCount(1);
        result[0].OrderStatus.Should().Be("Pending");
        result[0].UserId.Should().Be(userId);
    }

    // ── GetOrderReportRows (proyeccion escalar para reportes) ───────────────────

    [Fact]
    public async Task GetOrderReportRows_SinFiltro_RetornaTodasLasFilasEscalares()
    {
        using var context = CreateInMemoryContext();
        context.Orders.Add(CreateOrderReadModel());
        context.Orders.Add(CreateOrderReadModel());
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository.GetOrderReportRows().ToListAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.Total == 210 && r.OrderStatus == "Pending");
        result.Should().OnlyContain(r => r.BuyerEmail == "buyer@example.com");
    }

    [Fact]
    public async Task GetOrderReportRows_ConRangoDeFechas_FiltraPorCreatedAt()
    {
        using var context = CreateInMemoryContext();

        var dentro = CreateOrderReadModel();
        dentro.CreatedAt = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
        var antes = CreateOrderReadModel();
        antes.CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var despues = CreateOrderReadModel();
        despues.CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        context.Orders.AddRange(dentro, antes, despues);
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var result = await repository
            .GetOrderReportRows(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc))
            .ToListAsync();

        // 'to' es exclusivo: incluye 'dentro' (10 may), excluye 'antes' y 'despues' (1 jun).
        result.Should().HaveCount(1);
        result[0].CreatedAt.Should().Be(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetOrderReportRows_PermiteAgregacionGroupBy()
    {
        using var context = CreateInMemoryContext();
        var aprobada = CreateOrderReadModel();
        aprobada.OrderStatus = "Approved";
        var pendiente = CreateOrderReadModel();
        pendiente.OrderStatus = "Pending";
        context.Orders.AddRange(aprobada, pendiente);
        await context.SaveChangesAsync();

        var repository = new OrderReadRepository(context);

        var porEstado = await repository.GetOrderReportRows()
            .GroupBy(r => r.OrderStatus)
            .Select(g => new { g.Key, Count = g.Count(), Revenue = g.Sum(r => r.Total) })
            .ToListAsync();

        porEstado.Should().HaveCount(2);
        porEstado.Should().Contain(e => e.Key == "Approved" && e.Count == 1 && e.Revenue == 210);
    }
}
