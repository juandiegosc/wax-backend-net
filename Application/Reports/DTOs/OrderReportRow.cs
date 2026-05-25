namespace Application.Reports.DTOs;

/// <summary>
/// Proyeccion escalar de OrderReadModel para reportes.
/// Solo contiene campos escalares, sin deserializacion de JSON (OrderItems).
/// Permite que GroupBy/Sum/Count se traduzcan a SQL en PostgreSQL.
/// </summary>
public class OrderReportRow
{
    public required string OrderStatus { get; set; }
    public long Total { get; set; }
    public required string BuyerEmail { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
