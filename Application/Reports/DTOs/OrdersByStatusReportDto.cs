namespace Application.Reports.DTOs;

public class OrdersByStatusReportDto
{
    public required string Status { get; set; }
    public int Count { get; set; }
    public long Revenue { get; set; }
    public double Percentage { get; set; }
}
