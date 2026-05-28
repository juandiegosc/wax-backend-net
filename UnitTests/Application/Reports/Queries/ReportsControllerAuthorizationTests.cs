using System.Reflection;
using API.Controllers;
using Domain.Enumerators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests.Application.Reports.Queries;

public class ReportsControllerAuthorizationTests
{
    // RFC-03: todos los endpoints de reporteria son solo-Admin
    [Fact]
    public void ReportsController_TieneAuthorizeAdminANivelClase()
    {
        var attr = typeof(ReportsController).GetCustomAttribute<AuthorizeAttribute>();

        attr.Should().NotBeNull();
        attr!.Roles.Should().Be(Roles.Admin);
    }

    [Fact]
    public void NingunActionDeReportsController_PermiteAccesoAnonimo()
    {
        var acciones = typeof(ReportsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => typeof(IActionResult).IsAssignableFrom(UnwrapTaskReturn(m.ReturnType)));

        acciones.Should().NotBeEmpty();

        foreach (var accion in acciones)
        {
            accion.GetCustomAttribute<AllowAnonymousAttribute>()
                .Should().BeNull($"la accion {accion.Name} no debe permitir acceso anonimo");
        }
    }

    private static Type UnwrapTaskReturn(Type returnType)
    {
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return returnType.GetGenericArguments()[0];
        }
        return returnType;
    }
}
