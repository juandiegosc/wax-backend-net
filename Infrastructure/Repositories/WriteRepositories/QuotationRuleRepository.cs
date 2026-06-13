using Application.Interfaces.Repositories.WriteRepositories;
using Domain.ProductAggregate;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Repositories.WriteRepositories;

/// <summary>
/// Repositorio de escritura para QuotationRule.
/// Excepcion de dual-DB documentada (ADR-5): esta entidad es configuracion de bajo volumen;
/// las queries de lectura usan .AsNoTracking() directamente sobre WriteDbContext.
/// GetTrackedByIdAsync NO aplica AsNoTracking para que EF detecte cambios en mutaciones.
/// </summary>
public class QuotationRuleRepository(WriteDbContext context) : IQuotationRuleRepository
{
    public async Task<QuotationRule?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.QuotationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<QuotationRule?> GetTrackedByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.QuotationRules
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<QuotationRule?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return await context.QuotationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key, ct);
    }

    public async Task<bool> ExistsByKeyAsync(string key, CancellationToken ct = default)
    {
        return await context.QuotationRules
            .AnyAsync(r => r.Key == key, ct);
    }

    public async Task<IReadOnlyList<QuotationRule>> ListAsync(bool? activeOnly, CancellationToken ct = default)
    {
        var query = context.QuotationRules.AsNoTracking();

        if (activeOnly.HasValue)
            query = query.Where(r => r.IsActive == activeOnly.Value);

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(QuotationRule rule, CancellationToken ct = default)
    {
        await context.QuotationRules.AddAsync(rule, ct);
    }
}
