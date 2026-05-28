using Application.Reports.Queries;
using Application.Reports.Validators;
using FluentValidation.TestHelper;

namespace UnitTests.Application.Reports.Queries;

public class GetSupportByDateReportValidatorTests
{
    private readonly GetSupportByDateReportValidator _validator = new();

    private static GetSupportByDateReportQuery QueryValido() => new()
    {
        From = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        To = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Validate_CuandoFromMayorQueTo_TieneError()
    {
        var query = QueryValido();
        query.From = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        query.To = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void Validate_CuandoRangoValido_SinErrores()
    {
        var resultado = _validator.TestValidate(QueryValido());
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CuandoRangoMayorA366Dias_TieneError()
    {
        var query = QueryValido();
        query.From = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        query.To = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

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
