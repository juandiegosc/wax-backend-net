namespace Application.Reports.DTOs;

public class SupportSummaryReportDto
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Closed { get; set; }

    /// <summary>
    /// Conteo por categoria. Incluye todas las categorias del enum TicketCategory
    /// (OrderIssue, PaymentIssue, ProductIssue, Other), incluso si son 0.
    /// </summary>
    public Dictionary<string, int> ByCategory { get; set; } = [];
}
