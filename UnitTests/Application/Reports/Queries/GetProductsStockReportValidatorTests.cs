using Application.Reports.Queries;
using Application.Reports.Validators;
using FluentValidation.TestHelper;

namespace UnitTests.Application.Reports.Queries;

public class GetProductsStockReportValidatorTests
{
    private readonly GetProductsStockReportValidator _validator = new();

    // RFC-10-C: threshold invalido
    [Fact]
    public void Validate_CuandoThresholdNegativo_TieneError()
    {
        var query = new GetProductsStockReportQuery { Threshold = -1 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.Threshold);
    }

    [Fact]
    public void Validate_CuandoThresholdMuyNegativo_TieneError()
    {
        var query = new GetProductsStockReportQuery { Threshold = -100 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldHaveValidationErrorFor(x => x.Threshold);
    }

    // Valores validos
    [Fact]
    public void Validate_CuandoThresholdCero_SinErrores()
    {
        var query = new GetProductsStockReportQuery { Threshold = 0 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CuandoThresholdPorDefecto_SinErrores()
    {
        var query = new GetProductsStockReportQuery { Threshold = 10 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CuandoThresholdGrande_SinErrores()
    {
        var query = new GetProductsStockReportQuery { Threshold = 500 };

        var resultado = _validator.TestValidate(query);
        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
