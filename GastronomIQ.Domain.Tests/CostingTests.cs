using GastronomIQ.Domain.Costing;

namespace GastronomIQ.Domain.Tests;

public class CostingTests
{
    [Fact]
    public void Calculates_basic_recipe_cost()
    {
        var result = RecipeCostCalculator.Calculate(
            new[]
            {
                new CostIngredientInput(
                    Guid.NewGuid(),
                    2m,
                    1000m,
                    100m)
            },
            2m);

        Assert.Equal(200_000m, result.IngredientCost);
        Assert.Equal(100_000m, result.CostPerYieldUnit);
    }

    [Fact]
    public void Applies_waste_to_quantity_before_costing()
    {
        var result = RecipeCostCalculator.Calculate(
            new[]
            {
                new CostIngredientInput(
                    Guid.NewGuid(),
                    1m,
                    1m,
                    100m,
                    10m)
            },
            1m);

        Assert.Equal(110m, result.IngredientCost);
    }

    [Fact]
    public void Calculates_food_cost_percentage()
    {
        var result = RecipeCostCalculator.Calculate(
            new[]
            {
                new CostIngredientInput(
                    Guid.NewGuid(),
                    1m,
                    1m,
                    30m)
            },
            1m,
            100m);

        Assert.Equal(30m, result.FoodCostPercentage);
    }

    [Fact]
    public void Rejects_zero_yield()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeCostCalculator.Calculate(
                Array.Empty<CostIngredientInput>(),
                0));
    }
}
