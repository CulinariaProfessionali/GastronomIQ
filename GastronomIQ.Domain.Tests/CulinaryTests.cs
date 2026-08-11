using GastronomIQ.Domain.Culinary;

namespace GastronomIQ.Domain.Tests;

public class CulinaryTests
{
    [Fact]
    public void Unit_rejects_non_positive_conversion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Unit("kg", "Kilogram", UnitType.Weight, 0));
    }

    [Fact]
    public void Unit_converts_to_base()
    {
        var gram = new Unit("g", "Gram", UnitType.Weight, 1);
        Assert.Equal(250m, gram.ConvertToBase(250m));
    }

    [Fact]
    public void Ingredient_rejects_negative_cost()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Ingredient(
                Guid.NewGuid(),
                "Tomato",
                new UnitId(Guid.NewGuid()),
                -1));
    }

    [Fact]
    public void Ingredient_cost_is_deterministic()
    {
        Assert.Equal(125.5m, CostCalculator.CalculateIngredientCost(2.5m, 50.2m));
    }
}
