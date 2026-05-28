using System.Linq.Expressions;
using System.Text.Json;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Orders.DTOs;
using Application.Reports.DTOs;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.ReadModels;

namespace Infrastructure.Repositories.ReadRepositories;

public class OrderReadRepository(ReadDbContext context) : IOrderReadRepository
{
    public async Task<OrderDto?> GetOrderByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .Where(o => o.Id == id)
            .Select(MapToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OrderDto?> GetByPaymentIntentIdAsync(string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        return await context.Orders
            .Where(o => o.PaymentIntentId == paymentIntentId)
            .Select(MapToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    public IQueryable<OrderDto> GetQueryable(string? statusFilter = null, string? userId = null)
    {
        var query = context.Orders.AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
            query = query.Where(o => o.OrderStatus == statusFilter);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(o => o.UserId == userId);

        return query.Select(MapToDto);
    }
    
    public IQueryable<OrderReportRow> GetOrderReportRows(DateTime? from = null, DateTime? to = null)
    {
        var q = context.Orders.AsQueryable();

        if (from.HasValue)
            q = q.Where(o => o.CreatedAt >= from.Value);

        if (to.HasValue)
            q = q.Where(o => o.CreatedAt < to.Value);

        return q.Select(o => new OrderReportRow
        {
            OrderStatus = o.OrderStatus,
            Total = o.Total,
            BuyerEmail = o.BuyerEmail,
            UserId = o.UserId,
            CreatedAt = o.CreatedAt
        });
    }

    private static readonly Expression<Func<OrderReadModel, OrderDto>> MapToDto = o => new OrderDto
    {
        Id = o.Id,
        BuyerEmail = o.BuyerEmail,
        UserId = o.UserId,
        OrderStatus = o.OrderStatus,
        Subtotal = o.Subtotal,
        DeliveryFee = o.DeliveryFee,
        Total = o.Total,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        BillingAddress = new BillingAddressDto
        {
            Name = o.BillingName,
            Line1 = o.BillingLine1,
            Line2 = o.BillingLine2,
            City = o.BillingCity,
            State = o.BillingState,
            PostalCode = o.BillingPostalCode,
            Country = o.BillingCountry
        },
        PaymentSummary = new PaymentSummaryDto
        {
            Last4 = o.PaymentLast4,
            Brand = o.PaymentBrand,
            ExpMonth = o.PaymentExpMonth,
            ExpYear = o.PaymentExpYear
        },
        OrderItems = JsonSerializer.Deserialize<List<OrderItemDto>>(o.OrderItems, JsonSerializerOptions.Default) ??
                     new List<OrderItemDto>()
    };
    
}