namespace Infrastructure.Billing;

public class FacturaPlanSettings
{
    public string BaseUrl { get; set; } = "https://api.factuplan.com";
    public string ApiKey { get; set; } = string.Empty;
    public string TaxpayerRuc { get; set; } = string.Empty;
    public string Establishment { get; set; } = "001";
    public string EmissionPoint { get; set; } = "001";
    public string DefaultPaymentMethod { get; set; } = "19";
    public decimal TaxRate { get; set; } = 0.15m;
}
