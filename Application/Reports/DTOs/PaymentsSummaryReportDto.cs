namespace Application.Reports.DTOs;

public class PaymentsSummaryReportDto
{
    public int ConfirmedCount { get; set; }
    public long ConfirmedAmount { get; set; }
    public int FailedCount { get; set; }
    public long FailedAmount { get; set; }
    public int MismatchCount { get; set; }
    public long MismatchAmount { get; set; }
}
