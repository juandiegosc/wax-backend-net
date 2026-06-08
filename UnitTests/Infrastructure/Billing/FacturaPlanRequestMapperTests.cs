using Application.Billing.DTOs;
using Infrastructure.Billing;

namespace UnitTests.Infrastructure.Billing;

public class FacturaPlanRequestMapperTests
{
    private static FacturaPlanSettings DefaultSettings() => new()
    {
        BaseUrl = "https://api-rest.factuplan.com.ec",
        ApiKey = "test-key",
        TaxpayerRuc = "1234567890001",
        Establishment = "001",
        EmissionPoint = "001",
        DefaultPaymentMethod = "19",
        TaxRate = 15m
    };

    private static InvoiceRequest BuildRequest(
        string? legalName = "Test User",
        string email = "buyer@test.com",
        decimal totalAmount = 100.00m,
        decimal deliveryFee = 0m,
        IReadOnlyList<InvoiceLine>? items = null)
    {
        var defaultItems = items ?? new List<InvoiceLine>
        {
            new("PROD-1", "Product One", 2, 50.00m, 0m)
        };

        return new InvoiceRequest(
            OrderId: "order-1",
            PaymentIntentId: "pi-1",
            Customer: new InvoiceCustomer("9999999999", legalName ?? email, email, null),
            Items: defaultItems,
            Payment: new InvoicePayment(totalAmount, "USD"));
    }

    [Fact]
    public void Map_PaymentAmount_RecomputedFromItemsPlusTax()
    {
        var settings = DefaultSettings();
        var mapper = new FacturaPlanRequestMapper();
        var items = new List<InvoiceLine>
        {
            new("P1", "Product", 1, 100.00m, 0m)
        };
        var request = BuildRequest(totalAmount: 999.99m, items: items);

        var result = mapper.Map(request, settings);

        // subtotal 100 * (1 + 15/100) = 115.00, ignora el totalAmount del request
        result.Payments[0].Amount.Should().Be(115.00m);
    }

    [Fact]
    public void Map_ConvertsPricePerItemToDecimal_DividingBy100()
    {
        var settings = DefaultSettings();
        var mapper = new FacturaPlanRequestMapper();
        var items = new List<InvoiceLine>
        {
            new("PROD-1", "Item One", 1, 50.00m, 0m)
        };
        var request = BuildRequest(items: items);

        var result = mapper.Map(request, settings);

        result.Items[0].UnitPrice.Should().Be(50.00m);
    }

    [Fact]
    public void Map_BillingNameNull_UsesEmailAsLegalName()
    {
        var settings = DefaultSettings();
        var mapper = new FacturaPlanRequestMapper();
        var request = BuildRequest(legalName: null, email: "x@x.com");

        var result = mapper.Map(request, settings);

        result.Customer.LegalName.Should().Be("x@x.com");
    }

    [Fact]
    public void Map_BillingNamePresent_UsesNameAsLegalName()
    {
        var settings = DefaultSettings();
        var mapper = new FacturaPlanRequestMapper();
        var request = BuildRequest(legalName: "John", email: "john@test.com");

        var result = mapper.Map(request, settings);

        result.Customer.LegalName.Should().Be("John");
    }

    [Fact]
    public void Map_SetsIdentificationType_AsFinalConsumer()
    {
        var settings = DefaultSettings();
        var mapper = new FacturaPlanRequestMapper();
        var request = BuildRequest();

        var result = mapper.Map(request, settings);

        result.Customer.IdentificationType.Should().Be("FINAL_CONSUMER");
    }

    [Fact]
    public void Map_SetsIdentification_As9999999999()
    {
        var settings = DefaultSettings();
        var mapper = new FacturaPlanRequestMapper();
        var request = BuildRequest();

        var result = mapper.Map(request, settings);

        result.Customer.Identification.Should().Be("9999999999");
    }

    [Fact]
    public void Map_SetsTaxType_AsIvaRate_OnAllItems()
    {
        var settings = DefaultSettings();
        var mapper = new FacturaPlanRequestMapper();
        var items = new List<InvoiceLine>
        {
            new("P1", "Item 1", 1, 10.00m, 0m),
            new("P2", "Item 2", 2, 20.00m, 0m),
            new("P3", "Item 3", 1, 30.00m, 0m)
        };
        var request = BuildRequest(items: items);

        var result = mapper.Map(request, settings);

        result.Items.Should().AllSatisfy(item => item.TaxType.Should().Be("IVA_RATE"));
    }

    [Fact]
    public void Map_PaymentMethod_FromSettings()
    {
        var settings = DefaultSettings();
        settings.DefaultPaymentMethod = "19";
        var mapper = new FacturaPlanRequestMapper();
        var request = BuildRequest();

        var result = mapper.Map(request, settings);

        result.Payments[0].Method.Should().Be("19");
    }

    [Fact]
    public void Map_SetsTax_AsRatePercentage_NotAmount()
    {
        var settings = DefaultSettings();
        settings.TaxRate = 15m;
        var mapper = new FacturaPlanRequestMapper();
        var items = new List<InvoiceLine>
        {
            new("P1", "Taxable Product", 2, 50.00m, 0m)
        };
        var request = BuildRequest(items: items);

        var result = mapper.Map(request, settings);

        // FacturaPlan espera la tasa como porcentaje (15), no el monto calculado
        result.Items[0].Tax.Should().Be(15m);
    }
}
