namespace Application.Reports.DTOs;

public class ProductsStockReportDto
{
    public int TotalProducts { get; set; }
    public int OutOfStock { get; set; }
    public int LowStock { get; set; }
    public int InStock { get; set; }
}
