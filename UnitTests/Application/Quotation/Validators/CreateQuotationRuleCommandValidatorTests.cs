using Application.Quotation.Commands;
using Application.Quotation.DTOs;
using Application.Quotation.Validators;
using FluentValidation.TestHelper;

namespace UnitTests.Application.Quotation.Validators;

public class CreateQuotationRuleCommandValidatorTests
{
    private readonly CreateQuotationRuleCommandValidator _validator = new();

    private static CreateQuotationRuleCommand Build(string key = "VALID_KEY", decimal value = 10m, string? description = null)
        => new()
        {
            Dto = new CreateQuotationRuleDto
            {
                Key = key,
                Value = value,
                Description = description
            }
        };

    [Fact]
    public void Validate_ConKeyVacia_GeneraError()
    {
        var result = _validator.TestValidate(Build(key: ""));
        result.ShouldHaveValidationErrorFor(x => x.Dto.Key);
    }

    [Fact]
    public void Validate_ConKeyMayorA100Caracteres_GeneraError()
    {
        var result = _validator.TestValidate(Build(key: new string('A', 101)));
        result.ShouldHaveValidationErrorFor(x => x.Dto.Key);
    }

    [Fact]
    public void Validate_ConKeyDe100Caracteres_NoGeneraError()
    {
        var result = _validator.TestValidate(Build(key: new string('A', 100)));
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.Key);
    }

    [Theory]
    [InlineData(" LEADING")]
    [InlineData("TRAILING ")]
    [InlineData(" BOTH ")]
    public void Validate_ConKeyConEspaciosAlInicioOFinal_GeneraError(string key)
    {
        var result = _validator.TestValidate(Build(key: key));
        result.ShouldHaveValidationErrorFor(x => x.Dto.Key);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_ConValorMenorOIgualACero_GeneraError(decimal value)
    {
        var result = _validator.TestValidate(Build(value: value));
        result.ShouldHaveValidationErrorFor(x => x.Dto.Value);
    }

    [Fact]
    public void Validate_ConValorPositivo_NoGeneraError()
    {
        var result = _validator.TestValidate(Build(value: 0.01m));
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.Value);
    }

    [Fact]
    public void Validate_ConDescripcionMayorA500Caracteres_GeneraError()
    {
        var result = _validator.TestValidate(Build(description: new string('x', 501)));
        result.ShouldHaveValidationErrorFor(x => x.Dto.Description);
    }

    [Fact]
    public void Validate_ConDescripcionNull_NoGeneraError()
    {
        var result = _validator.TestValidate(Build(description: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.Description);
    }

    [Fact]
    public void Validate_ComandoValido_SinErrores()
    {
        var result = _validator.TestValidate(Build(key: "BASE_COST", value: 100m, description: "ok"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
