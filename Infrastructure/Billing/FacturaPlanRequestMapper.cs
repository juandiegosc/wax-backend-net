using Application.Billing.DTOs;

namespace Infrastructure.Billing;

public class FacturaPlanRequestMapper
{
    internal FacturaPlanInvoiceRequest Map(InvoiceRequest request, FacturaPlanSettings settings)
    {
        var customer = new FacturaPlanCustomer(
            IdentificationType: "FINAL_CONSUMER",
            Identification: "9999999999",
            LegalName: request.Customer.LegalName,
            Email: request.Customer.Email,
            Address: request.Customer.Address);

        // settings.TaxRate is a percentage (e.g. 15 = 15%). Send it verbatim in items[].tax
        // and divide by 100 only when computing the gross total.
        var items = request.Items.Select(line => new FacturaPlanItem(
            Quantity: line.Quantity,
            Code: line.Code,
            Description: line.Description,
            UnitPrice: line.UnitPrice,
            TaxType: "IVA_RATE",
            Tax: settings.TaxRate)).ToList();

        var subtotal = items.Sum(i => i.UnitPrice * i.Quantity);
        var computedTotal = decimal.Round(subtotal * (1m + settings.TaxRate / 100m), 2, MidpointRounding.AwayFromZero);

        var payments = new List<FacturaPlanPayment>
        {
            new(Method: settings.DefaultPaymentMethod, Amount: computedTotal)
        };

        return new FacturaPlanInvoiceRequest(
            Customer: customer,
            Items: items,
            Payments: payments,
            Establishment: settings.Establishment,
            EmissionPoint: settings.EmissionPoint);
    }
}
