using Application.Quotation.Commands;
using Application.Quotation.DTOs;
using Application.Quotation.Validators;
using FluentValidation.TestHelper;

namespace UnitTests.Application.Quotation.Validators;

public class UpdateQuotationRuleCommandValidatorTests
{
    private readonly UpdateQuotationRuleCommandValidator _validator = new();

    private static UpdateQuotationRuleCommand Build(string id = "rule-1", decimal value = 10m, string? description = null, bool isActive = true)
        => new()
        {
            Id = id,
            Dto = new UpdateQuotationRuleDto
            {
                Value = value,
                Description = description,
                IsActive = isActive
            }
        };

    [Fact]
    public void Validate_ConIdVacio_GeneraError()
    {
        var result = _validator.TestValidate(Build(id: ""));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void Validate_ConValorMenorOIgualACero_GeneraError(decimal value)
    {
        var result = _validator.TestValidate(Build(value: value));
        result.ShouldHaveValidationErrorFor(x => x.Dto.Value);
    }

    [Fact]
    public void Validate_ConValorPositivo_NoGeneraError()
    {
        var result = _validator.TestValidate(Build(value: 0.5m));
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
        var result = _validator.TestValidate(Build(id: "rule-1", value: 99m, description: "ok", isActive: false));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
