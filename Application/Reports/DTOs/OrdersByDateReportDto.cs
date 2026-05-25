namespace Application.Reports.DTOs;

public class OrdersByDateReportDto
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public long Revenue { get; set; }
}
