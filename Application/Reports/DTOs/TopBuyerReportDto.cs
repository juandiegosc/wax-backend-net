namespace Application.Reports.DTOs;

public class TopBuyerReportDto
{
    public required string BuyerEmail { get; set; }
    public string? UserId { get; set; }
    public int OrderCount { get; set; }
    public long TotalRevenue { get; set; }
}
