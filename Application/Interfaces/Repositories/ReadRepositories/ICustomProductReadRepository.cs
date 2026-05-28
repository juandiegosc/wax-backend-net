using Application.CustomProducts.DTOs;
using Application.Reports.DTOs;

namespace Application.Interfaces.Repositories.ReadRepositories;

public interface ICustomProductReadRepository
{
    Task<CustomProductDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    IQueryable<CustomProductDto> GetQueryable();
    IQueryable<CustomProductDto> GetByOwner(string ownerUserId);

    /// <summary>
    /// Proyeccion escalar para reportes: solo Status y AgreedPrice.
    /// SQL-traducible: evita construir Design y List&lt;Proposals&gt; en cliente.
    /// </summary>
    IQueryable<CustomProductReportRow> GetCustomProductReportRows();
}
