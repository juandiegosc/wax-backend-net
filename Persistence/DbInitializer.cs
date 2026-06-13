using Domain.Entities;
using Domain.Enumerators;
using Domain.ProductAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Persistence;

public class DbInitializer(
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    WriteDbContext context,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedQuotationRulesAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(role)) continue;
            var result = await roleManager.CreateAsync(new IdentityRole(role));
                
            if (!result.Succeeded) logger.LogError("Role {Role} not found", role);
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];

        if (string.IsNullOrEmpty(adminEmail) && string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("Admin:Email o Admin:Password no configurados. Seed de admin omitido.");
            return;
        }
        
        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
        };
            
        var result = await userManager.CreateAsync(admin, adminPassword!);
        if (!result.Succeeded) logger.LogError("Failed while creating user {User}", adminEmail);

        await userManager.AddToRoleAsync(admin, Roles.Admin);
    }

    private async Task SeedQuotationRulesAsync()
    {
        if (!await context.QuotationRules.AnyAsync())
        {
            var base_cost = new QuotationRule { Key = "BASE_COST", Value = 5000m, Description = "Costo base en centavos" };
            base_cost.MarkAsDefault();
            var margin = new QuotationRule { Key = "MARGIN_MULTIPLIER", Value = 1.6m, Description = "Multiplicador de margen" };
            margin.MarkAsDefault();
            var depth = new QuotationRule { Key = "DEFAULT_DEPTH_CM", Value = 5m, Description = "Profundidad por defecto" };
            depth.MarkAsDefault();
            var material = new QuotationRule { Key = "MATERIAL_default", Value = 2m, Description = "Costo en centavos por cm3" };
            material.MarkAsDefault();

            context.QuotationRules.AddRange(base_cost, margin, depth, material);
            await context.SaveChangesAsync();
        }
        else
        {
            // Backfill idempotente: marcar las reglas semilla que aun no tengan IsDefault = true.
            var defaultKeys = new[] { "BASE_COST", "MARGIN_MULTIPLIER", "DEFAULT_DEPTH_CM", "MATERIAL_default" };
            var rulesWithoutDefault = await context.QuotationRules
                .Where(r => defaultKeys.Contains(r.Key) && !r.IsDefault)
                .ToListAsync();

            if (rulesWithoutDefault.Count > 0)
            {
                foreach (var rule in rulesWithoutDefault)
                    rule.MarkAsDefault();

                await context.SaveChangesAsync();
            }
        }
    }
}