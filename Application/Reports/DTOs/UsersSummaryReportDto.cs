namespace Application.Reports.DTOs;

public class UsersSummaryReportDto
{
    public int TotalUsers { get; set; }
    public int ConfirmedEmails { get; set; }

    /// <summary>
    /// Conteo de usuarios por rol (Admin, Registered, Enrolled).
    /// </summary>
    public Dictionary<string, int> ByRole { get; set; } = [];
}
