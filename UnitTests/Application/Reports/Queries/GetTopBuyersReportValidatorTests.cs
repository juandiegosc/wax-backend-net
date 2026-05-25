using Application.Reports.Queries;
using Application.Reports.Validators;
using FluentValidation.TestHelper;

namespace UnitTests.Application.Reports.Queries;

public class GetTopBuyersReportValidatorTests
{
    private readonly GetTopBuyersReportValidator _validator = new();

    // RFC-08-C: limit invalido
    [Fact]
    public void Validate_CuandoLimitCero_TieneError()
    {
        var query = new GetTopBuyersReportQuery { Limit = 0 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void Validate_CuandoLimitNegativo_TieneError()
    {
        var query = new GetTopBuyersReportQuery { Limit = -3 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void Validate_CuandoLimitMayorACien_TieneError()
    {
        var query = new GetTopBuyersReportQuery { Limit = 101 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void Validate_CuandoLimitValido_SinErrores()
    {
        var query = new GetTopBuyersReportQuery { Limit = 5 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CuandoLimitCienExacto_SinErrores()
    {
        var query = new GetTopBuyersReportQuery { Limit = 100 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
