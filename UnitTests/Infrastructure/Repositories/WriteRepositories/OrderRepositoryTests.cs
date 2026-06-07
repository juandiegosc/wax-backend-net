using Domain.Entities;
using Domain.OrderAggregate;
using Infrastructure.Repositories.WriteRepositories;
using Microsoft.EntityFrameworkCore;
using Persistence;
using BillingAddress = Domain.Entities.BillingAddress;

namespace UnitTests.Infrastructure.Repositories.WriteRepositories;

public class OrderRepositoryTests
{
    private static WriteDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WriteDbContext(options);
    }

    private static Order CreateOrder(string? id = null, string? paymentIntentId = null)
    {
        var addressId = Guid.NewGuid().ToString();
        return new Order
        {
            Id = id ?? Guid.NewGuid().ToString(),
            BuyerEmail = "test@example.com",
            BillingAddressId = addressId,
            BillingAddress = new BillingAddress
            {
                Id = addressId,
                Name = "John Doe",
                Line1 = "123 Main St",
                City = "Testville",
                State = "TS",
                PostalCode = "12345",
                Country = "US"
            },
            PaymentSummary = new PaymentSummary
            {
                Last4 = 4242,
                Brand = "Visa",
                ExpMonth = 12,
                ExpYear = 2025
            },
            Subtotal = 10000,
            DeliveryFee = 500,
            PaymentIntentId = paymentIntentId ?? "pi_test_123"
        };
    }

    [Fact]
    public async Task GetQueryable_ReturnsAllOrders()
    {
        using var context = CreateInMemoryContext();
        context.Orders.Add(CreateOrder());
        context.Orders.Add(CreateOrder());
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        var result = await repository.GetQueryable().ToListAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByPaymentIntentIdAsync_WhenOrderExists_ReturnsOrder()
    {
        using var context = CreateInMemoryContext();
        var paymentIntentId = "pi_specific_test";
        var order = CreateOrder(paymentIntentId: paymentIntentId);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        var result = await repository.GetByPaymentIntentIdAsync(paymentIntentId);

        result.Should().NotBeNull();
        result!.PaymentIntentId.Should().Be(paymentIntentId);
    }

    [Fact]
    public async Task GetByPaymentIntentIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repository = new OrderRepository(context);

        var result = await repository.GetByPaymentIntentIdAsync("non-existent-pi");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenOrderExists_ReturnsOrder()
    {
        using var context = CreateInMemoryContext();
        var orderId = Guid.NewGuid().ToString();
        var order = CreateOrder(id: orderId);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        var result = await repository.GetByOrderIdAsync(orderId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repository = new OrderRepository(context);

        var result = await repository.GetByOrderIdAsync("non-existent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Add_AddsOrderToContext()
    {
        using var context = CreateInMemoryContext();
        var repository = new OrderRepository(context);
        var order = CreateOrder();

        repository.Add(order);
        await context.SaveChangesAsync();

        var stored = await context.Orders.FindAsync(order.Id);
        stored.Should().NotBeNull();
        stored!.BuyerEmail.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByPaymentIntentIdAsync_IncludesOrderItems()
    {
        using var context = CreateInMemoryContext();
        var paymentIntentId = "pi_with_items";
        var order = CreateOrder(paymentIntentId: paymentIntentId);
        order.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid().ToString(),
            ItemOrdered = new ProductOrderItem
            {
                ProductId = "prod-1",
                Name = "Item 1"
            },
            Price = 5000,
            Quantity = 2
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        var result = await repository.GetByPaymentIntentIdAsync(paymentIntentId);

        result.Should().NotBeNull();
        result!.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByPaymentIntentIdAsync_IncludesBillingAddress()
    {
        using var context = CreateInMemoryContext();
        var paymentIntentId = "pi_billing_test";
        var order = CreateOrder(paymentIntentId: paymentIntentId);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        var result = await repository.GetByPaymentIntentIdAsync(paymentIntentId);

        result.Should().NotBeNull();
        result!.BillingAddress.Should().NotBeNull();
        result.BillingAddress.Name.Should().Be("John Doe");
    }
}
