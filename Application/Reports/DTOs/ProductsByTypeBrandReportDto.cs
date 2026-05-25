namespace Application.Reports.DTOs;

public class ProductsByTypeBrandReportDto
{
    public required string Type { get; set; }
    public required string Brand { get; set; }
    public int Count { get; set; }
}
