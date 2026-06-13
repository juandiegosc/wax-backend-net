using Application.Billing.DTOs;
using Application.Reports.DTOs;
using Application.Reports.Queries;
using Application.Reports.Queries.Invoices;
using Domain.Entities;
using Domain.Enumerators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize(Roles = Roles.Admin)]
public class ReportsController(UserManager<User> userManager) : BaseApiController
{
    // -------------------------------------------------------------------------
    // Ordenes
    // -------------------------------------------------------------------------

    [HttpGet("orders/summary")]
    [ProducesResponseType(typeof(OrdersSummaryReportDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetOrdersSummary()
        => await HandleQuery<OrdersSummaryReportDto>(new GetOrdersSummaryReportQuery());

    [HttpGet("orders/by-date")]
    [ProducesResponseType(typeof(List<OrdersByDateReportDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetOrdersByDate(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] DateGranularity granularity = DateGranularity.Day)
        => await HandleQuery<List<OrdersByDateReportDto>>(
            new GetOrdersByDateReportQuery { From = from, To = to, Granularity = granularity });

    [HttpGet("orders/by-status")]
    [ProducesResponseType(typeof(List<OrdersByStatusReportDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetOrdersByStatus()
        => await HandleQuery<List<OrdersByStatusReportDto>>(new GetOrdersByStatusReportQuery());

    [HttpGet("orders/top-buyers")]
    [ProducesResponseType(typeof(List<TopBuyerReportDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetTopBuyers([FromQuery] int limit = 10)
        => await HandleQuery<List<TopBuyerReportDto>>(new GetTopBuyersReportQuery { Limit = limit });

    // -------------------------------------------------------------------------
    // Pagos
    // -------------------------------------------------------------------------

    [HttpGet("payments/summary")]
    [ProducesResponseType(typeof(PaymentsSummaryReportDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetPaymentsSummary()
        => await HandleQuery<PaymentsSummaryReportDto>(new GetPaymentsSummaryReportQuery());

    // -------------------------------------------------------------------------
    // Productos catalogo
    // -------------------------------------------------------------------------

    [HttpGet("products/stock")]
    [ProducesResponseType(typeof(ProductsStockReportDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetProductsStock([FromQuery] int threshold = 10)
        => await HandleQuery<ProductsStockReportDto>(new GetProductsStockReportQuery { Threshold = threshold });

    [HttpGet("products/by-type-brand")]
    [ProducesResponseType(typeof(List<ProductsByTypeBrandReportDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetProductsByTypeBrand()
        => await HandleQuery<List<ProductsByTypeBrandReportDto>>(new GetProductsByTypeBrandReportQuery());

    // -------------------------------------------------------------------------
    // Custom Products
    // -------------------------------------------------------------------------

    [HttpGet("custom-products/funnel")]
    [ProducesResponseType(typeof(List<CustomProductsFunnelReportDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetCustomProductsFunnel()
        => await HandleQuery<List<CustomProductsFunnelReportDto>>(new GetCustomProductsFunnelReportQuery());

    [HttpGet("custom-products/price-stats")]
    [ProducesResponseType(typeof(CustomProductsPriceStatsReportDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetCustomProductsPriceStats()
        => await HandleQuery<CustomProductsPriceStatsReportDto>(new GetCustomProductsPriceStatsReportQuery());

    // -------------------------------------------------------------------------
    // Soporte
    // -------------------------------------------------------------------------

    [HttpGet("support/summary")]
    [ProducesResponseType(typeof(SupportSummaryReportDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetSupportSummary()
        => await HandleQuery<SupportSummaryReportDto>(new GetSupportSummaryReportQuery());

    [HttpGet("support/by-date")]
    [ProducesResponseType(typeof(List<SupportByDateReportDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetSupportByDate(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
        => await HandleQuery<List<SupportByDateReportDto>>(
            new GetSupportByDateReportQuery { From = from, To = to });

    // -------------------------------------------------------------------------
    // Facturas (FacturaPlan pass-through)
    // -------------------------------------------------------------------------

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(InvoiceListResult), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(502)]
    public async Task<ActionResult> ListInvoices()
        => await HandleQuery<InvoiceListResult>(new GetInvoicesListQuery());

    [HttpGet("invoices/{id}")]
    [ProducesResponseType(typeof(InvoiceDetail), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(502)]
    public async Task<ActionResult> GetInvoice(string id)
        => await HandleQuery<InvoiceDetail>(new GetInvoiceByIdQuery(id));

    // -------------------------------------------------------------------------
    // Usuarios
    // -------------------------------------------------------------------------
    // Excepcion documentada: la informacion de usuarios vive en ASP.NET Identity
    // (UserManager pertenece a Infrastructure/Identity). Resolver esto via un handler
    // CQRS obligaria a inyectar UserManager en Application, violando la direccion de
    // capas. Por eso la logica se mantiene en el controlador, igual que AdminController.

    [HttpGet("users/summary")]
    [ProducesResponseType(typeof(UsersSummaryReportDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetUsersSummary()
    {
        var users = await userManager.Users.ToListAsync();

        var byRole = new Dictionary<string, int>
        {
            [Roles.Admin] = 0,
            [Roles.Registered] = 0,
            [Roles.Enrolled] = 0
        };

        var confirmedEmails = 0;
        foreach (var user in users)
        {
            if (user.EmailConfirmed)
            {
                confirmedEmails++;
            }

            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                byRole[role] = byRole.TryGetValue(role, out var actual) ? actual + 1 : 1;
            }
        }

        var dto = new UsersSummaryReportDto
        {
            TotalUsers = users.Count,
            ConfirmedEmails = confirmedEmails,
            ByRole = byRole
        };

        return Ok(dto);
    }
}
