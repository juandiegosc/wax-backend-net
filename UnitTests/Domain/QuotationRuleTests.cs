using Domain.ProductAggregate;

namespace UnitTests.Domain;

public class QuotationRuleTests
{
    private static QuotationRule CreateRule(decimal value = 10m, bool isActive = true, bool isDefault = false)
    {
        var rule = new QuotationRule
        {
            Key = "TEST_KEY",
            Value = value,
            IsActive = isActive
        };

        if (isDefault)
            rule.MarkAsDefault();

        return rule;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UpdateValue_ConValorMenorOIgualACero_LanzaExcepcion(decimal valor)
    {
        var rule = CreateRule();

        var act = () => rule.UpdateValue(valor);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateValue_ConValorPositivo_ActualizaValor()
    {
        var rule = CreateRule(value: 5m);

        rule.UpdateValue(99m);

        rule.Value.Should().Be(99m);
    }

    [Fact]
    public void Deactivate_EstableceIsActiveEnFalse()
    {
        var rule = CreateRule(isActive: true);

        rule.Deactivate();

        rule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_EstableceIsActiveEnTrue()
    {
        var rule = CreateRule(isActive: false);

        rule.Activate();

        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MarkAsDefault_EstableceIsDefaultEnTrue()
    {
        var rule = CreateRule(isDefault: false);

        rule.MarkAsDefault();

        rule.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void UpdateDescription_ActualizaDescripcion()
    {
        var rule = CreateRule();

        rule.UpdateDescription("Nueva descripcion");

        rule.Description.Should().Be("Nueva descripcion");
    }

    [Fact]
    public void UpdateDescription_ConNull_EstableceNull()
    {
        var rule = CreateRule();
        rule.UpdateDescription("desc original");

        rule.UpdateDescription(null);

        rule.Description.Should().BeNull();
    }
}
