using Application.Quotation.Commands;
using Application.Quotation.Validators;
using FluentValidation.TestHelper;

namespace UnitTests.Application.Quotation.Validators;

public class DeleteQuotationRuleCommandValidatorTests
{
    private readonly DeleteQuotationRuleCommandValidator _validator = new();

    [Fact]
    public void Validate_ConIdVacio_GeneraError()
    {
        var command = new DeleteQuotationRuleCommand { Id = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_ConIdValido_SinErrores()
    {
        var command = new DeleteQuotationRuleCommand { Id = "rule-1" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
