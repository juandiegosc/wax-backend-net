using Application.Reports.DTOs;
using Application.Reports.Queries;
using Application.Reports.Validators;
using FluentValidation.TestHelper;

namespace UnitTests.Application.Reports.Queries;

public class GetOrdersByDateReportValidatorTests
{
    private readonly GetOrdersByDateReportValidator _validator = new();

    private static GetOrdersByDateReportQuery QueryValido() => new()
    {
        From = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        To = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
        Granularity = DateGranularity.Day
    };

    // RFC-04-A: From > To invalido
    [Fact]
    public void Validate_CuandoFromMayorQueTo_TieneError()
    {
        var query = QueryValido();
        query.From = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        query.To = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.From);
    }

    // RFC-04-B: rango valido pasa
    [Fact]
    public void Validate_CuandoRangoValido_SinErrores()
    {
        var query = QueryValido();

        var resultado = _validator.TestValidate(query);
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    // Granularidad no soportada (distinta de Day en v1)
    [Fact]
    public void Validate_CuandoRangoMayorA366Dias_TieneError()
    {
        var query = QueryValido();
        query.From = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        query.To = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc); // mas de 366 dias

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void Validate_CuandoFromIgualTo_EsValido()
    {
        var query = QueryValido();
        query.From = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        query.To = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var resultado = _validator.TestValidate(query);
        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
