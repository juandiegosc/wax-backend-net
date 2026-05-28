using System.Linq.Expressions;
using Application.CustomProducts.DTOs;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Reports.DTOs;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.ReadModels;

namespace Infrastructure.Repositories.ReadRepositories;

public class CustomProductReadRepository(ReadDbContext context) : ICustomProductReadRepository
{
    public  Task<CustomProductDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => context.CustomProducts.Where(p => p.Id == id).Select(MapToDto).FirstOrDefaultAsync(cancellationToken);

    public IQueryable<CustomProductDto> GetQueryable() => context.CustomProducts.Select(MapToDto);

    public IQueryable<CustomProductDto> GetByOwner(string ownerUserId)
        => context.CustomProducts.Where(p => p.OwnerUserId == ownerUserId).Select(MapToDto);

    /// <inheritdoc/>
    public IQueryable<CustomProductReportRow> GetCustomProductReportRows()
        => context.CustomProducts.Select(p => new CustomProductReportRow
        {
            Status     = p.Status,
            AgreedPrice = p.AgreedPrice
        });

    private static readonly Expression<Func<CustomProductReadModel, CustomProductDto>> MapToDto = p => new CustomProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        TaskId = p.TaskId,
        GlbUrl = p.GlbUrl,
        OwnerUserId = p.OwnerUserId,
        Status = p.Status,
        AgreedPrice = p.AgreedPrice,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Design = new CustomProductDesignRequest
        {
            Type = p.DesignType,
            Material = p.DesignMaterial,
            Color = p.DesignColor,
            Shape = p.DesignShape,
            Dimensions = p.DesignDimensions,
            Details = p.DesignDetails
        },
        Proposals = new List<PriceProposalResponse>()
    };
}