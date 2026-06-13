using Domain.ProductAggregate;

namespace Application.Interfaces.Repositories.WriteRepositories;

public interface IQuotationRuleRepository
{
    Task<QuotationRule?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<QuotationRule?> GetTrackedByIdAsync(string id, CancellationToken ct = default);
    Task<QuotationRule?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<QuotationRule>> ListAsync(bool? activeOnly, CancellationToken ct = default);
    Task AddAsync(QuotationRule rule, CancellationToken ct = default);
}
