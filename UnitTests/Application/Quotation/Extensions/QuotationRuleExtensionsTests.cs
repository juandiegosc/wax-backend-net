using Application.Quotation.DTOs;
using Application.Quotation.Extensions;
using Domain.ProductAggregate;

namespace UnitTests.Application.Quotation.Extensions;

public class QuotationRuleExtensionsTests
{
    [Fact]
    public void ToEntity_MapeaCamposYDejaIsActiveEnTrue()
    {
        var dto = new CreateQuotationRuleDto
        {
            Key = "BASE_COST",
            Value = 5000m,
            Description = "Costo base"
        };

        var entity = dto.ToEntity();

        entity.Key.Should().Be("BASE_COST");
        entity.Value.Should().Be(5000m);
        entity.Description.Should().Be("Costo base");
        entity.IsActive.Should().BeTrue();
        entity.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void ToEntity_ConDescripcionNull_MantieneNull()
    {
        var dto = new CreateQuotationRuleDto
        {
            Key = "KEY",
            Value = 1m,
            Description = null
        };

        var entity = dto.ToEntity();

        entity.Description.Should().BeNull();
    }

    [Fact]
    public void ApplyTo_ConIsActiveTrue_ActualizaCamposYActiva()
    {
        var rule = new QuotationRule { Key = "KEY", Value = 10m, IsActive = false };
        var dto = new UpdateQuotationRuleDto
        {
            Value = 99m,
            Description = "nueva",
            IsActive = true
        };

        dto.ApplyTo(rule);

        rule.Value.Should().Be(99m);
        rule.Description.Should().Be("nueva");
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ApplyTo_ConIsActiveFalse_Desactiva()
    {
        var rule = new QuotationRule { Key = "KEY", Value = 10m, IsActive = true };
        var dto = new UpdateQuotationRuleDto
        {
            Value = 20m,
            Description = null,
            IsActive = false
        };

        dto.ApplyTo(rule);

        rule.IsActive.Should().BeFalse();
        rule.Description.Should().BeNull();
        rule.Value.Should().Be(20m);
    }

    [Fact]
    public void ApplyTo_NoModificaKeyNiIsDefault()
    {
        var rule = new QuotationRule { Key = "ORIGINAL_KEY", Value = 10m, IsActive = true };
        rule.MarkAsDefault();
        var dto = new UpdateQuotationRuleDto { Value = 50m, Description = "x", IsActive = true };

        dto.ApplyTo(rule);

        rule.Key.Should().Be("ORIGINAL_KEY");
        rule.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ToDto_MapeaTodosLosCampos()
    {
        var rule = new QuotationRule
        {
            Key = "MARGIN_MULTIPLIER",
            Value = 1.6m,
            Description = "Multiplicador",
            IsActive = true
        };
        rule.MarkAsDefault();

        var dto = rule.ToDto();

        dto.Id.Should().Be(rule.Id);
        dto.Key.Should().Be("MARGIN_MULTIPLIER");
        dto.Value.Should().Be(1.6m);
        dto.Description.Should().Be("Multiplicador");
        dto.IsActive.Should().BeTrue();
        dto.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ConIsDefaultFalse_RetornaFalse()
    {
        var rule = new QuotationRule { Key = "USER_RULE", Value = 5m, IsActive = false };

        var dto = rule.ToDto();

        dto.IsDefault.Should().BeFalse();
        dto.IsActive.Should().BeFalse();
    }
}
