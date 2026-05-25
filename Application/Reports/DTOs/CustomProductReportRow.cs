namespace Application.Reports.DTOs;

/// <summary>
/// Proyeccion escalar de CustomProductReadModel para reportes.
/// Solo contiene Status y AgreedPrice, sin construir el objeto Design ni la lista Proposals.
/// Permite que GroupBy/Count/Average se traduzcan a SQL en PostgreSQL.
/// </summary>
public class CustomProductReportRow
{
    public required string Status { get; set; }
    public long? AgreedPrice { get; set; }
}
