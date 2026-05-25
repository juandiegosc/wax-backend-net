namespace Application.Reports.DTOs;

public class CustomProductsPriceStatsReportDto
{
    public int ApprovedCount { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public long? AveragePrice { get; set; }
}
