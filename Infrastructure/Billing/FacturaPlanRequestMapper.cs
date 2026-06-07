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

        var items = request.Items.Select(line =>
        {
            var tax = line.UnitPrice * line.Quantity * settings.TaxRate;
            return new FacturaPlanItem(
                Quantity: line.Quantity,
                Code: line.Code,
                Description: line.Description,
                UnitPrice: line.UnitPrice,
                TaxType: "IVA_RATE",
                Tax: tax);
        }).ToList();

        var payments = new List<FacturaPlanPayment>
        {
            new(Method: settings.DefaultPaymentMethod, Amount: request.Payment.Amount)
        };

        return new FacturaPlanInvoiceRequest(
            Customer: customer,
            Items: items,
            Payments: payments,
            Establishment: settings.Establishment,
            EmissionPoint: settings.EmissionPoint,
            SendEmail: true);
    }
}
