namespace GastronomIQ.Domain.Costing;

public sealed record CostIngredientInput(
    Guid IngredientId,
    decimal RecipeQuantity,
    decimal UnitConversionToBase,
    decimal UnitCost,
    decimal WastePercentage = 0);

public sealed record CostIngredientResult(
    Guid IngredientId,
    decimal RecipeQuantity,
    decimal BaseQuantity,
    decimal WasteAdjustedQuantity,
    decimal UnitCost,
    decimal ExtendedCost);

public sealed record RecipeCostResult(
    decimal IngredientCost,
    decimal CostPerYieldUnit,
    decimal? SellingPrice,
    decimal? FoodCostPercentage,
    IReadOnlyList<CostIngredientResult> Ingredients);

public static class RecipeCostCalculator
{
    public static RecipeCostResult Calculate(
        IEnumerable<CostIngredientInput> ingredients,
        decimal yieldQuantity,
        decimal? sellingPrice = null)
    {
        if (yieldQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(yieldQuantity));

        if (sellingPrice is < 0)
            throw new ArgumentOutOfRangeException(nameof(sellingPrice));

        var results = ingredients.Select(CalculateIngredient).ToArray();

        var total = decimal.Round(
            results.Sum(x => x.ExtendedCost),
            4,
            MidpointRounding.AwayFromZero);

        var perYield = decimal.Round(
            total / yieldQuantity,
            4,
            MidpointRounding.AwayFromZero);

        decimal? foodCost = null;
        if (sellingPrice is > 0)
        {
            foodCost = decimal.Round(
                perYield / sellingPrice.Value * 100m,
                2,
                MidpointRounding.AwayFromZero);
        }

        return new RecipeCostResult(
            total,
            perYield,
            sellingPrice,
            foodCost,
            results);
    }

    private static CostIngredientResult CalculateIngredient(
        CostIngredientInput input)
    {
        if (input.RecipeQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(input.RecipeQuantity));

        if (input.UnitConversionToBase <= 0)
            throw new ArgumentOutOfRangeException(nameof(input.UnitConversionToBase));

        if (input.UnitCost < 0)
            throw new ArgumentOutOfRangeException(nameof(input.UnitCost));

        if (input.WastePercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(input.WastePercentage));

        var baseQuantity =
            input.RecipeQuantity * input.UnitConversionToBase;

        var wasteFactor =
            1m + (input.WastePercentage / 100m);

        var adjusted =
            baseQuantity * wasteFactor;

        var extended =
            decimal.Round(
                adjusted * input.UnitCost,
                4,
                MidpointRounding.AwayFromZero);

        return new CostIngredientResult(
            input.IngredientId,
            input.RecipeQuantity,
            decimal.Round(baseQuantity, 6),
            decimal.Round(adjusted, 6),
            input.UnitCost,
            extended);
    }
}
