namespace Application.Reports.DTOs;

public class OrdersSummaryReportDto
{
    public int TotalOrders { get; set; }
    public long TotalRevenue { get; set; }
    public long AverageTicket { get; set; }
    public List<OrdersByStatusReportDto> ByStatus { get; set; } = [];
}
