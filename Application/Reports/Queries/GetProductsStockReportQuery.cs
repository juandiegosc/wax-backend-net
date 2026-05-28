using Application.Core.Validations;
using Application.Reports.DTOs;
using MediatR;

namespace Application.Reports.Queries;

public class GetProductsStockReportQuery : IRequest<Result<ProductsStockReportDto>>
{
    public int Threshold { get; set; } = 10;
}
