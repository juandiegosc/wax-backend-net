using Application.Interfaces.Repositories.WriteRepositories;
using Domain.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Repositories.WriteRepositories;

public class OrderRepository(WriteDbContext context) : IOrderRepository
{
    public IQueryable<Order> GetQueryable()
    {
        return context.Orders.AsQueryable();
    }

    public async Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .Include(x => x.OrderItems)
            .Include(x => x.BillingAddress)
            .FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntentId, cancellationToken);
    }

    public async Task<Order?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .FindAsync([orderId], cancellationToken);
    }

    public void Add(Order order)
    {
        context.Orders.Add(order);
    }
    
}
