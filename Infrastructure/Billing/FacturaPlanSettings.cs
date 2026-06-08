namespace Infrastructure.Billing;

public class FacturaPlanSettings
{
    public string BaseUrl { get; set; } = "https://api-rest.factuplan.com.ec";
    public string ApiKey { get; set; } = string.Empty;
    public string TaxpayerRuc { get; set; } = string.Empty;
    public string Establishment { get; set; } = "001";
    public string EmissionPoint { get; set; } = "001";
    public string DefaultPaymentMethod { get; set; } = "19";
    // IVA expressed as percentage (e.g. 15 means 15%), to mirror exactly what FacturaPlan
    // expects in items[].tax. Internal multiplications use TaxRate / 100m.
    public decimal TaxRate { get; set; } = 15m;
}
