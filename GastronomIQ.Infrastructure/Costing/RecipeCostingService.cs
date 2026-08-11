using GastronomIQ.Application.Costing;
using GastronomIQ.Domain.Costing;

namespace GastronomIQ.Infrastructure.Costing;

public sealed class RecipeCostingService : IRecipeCostingService
{
    public Task<RecipeCostResultDto> CalculateAsync(
        CostRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var inputs = request.Ingredients.Select(x =>
            new CostIngredientInput(
                x.IngredientId,
                x.RecipeQuantity,
                x.UnitConversionToBase,
                x.UnitCost,
                x.WastePercentage));

        var result = RecipeCostCalculator.Calculate(
            inputs,
            request.YieldQuantity,
            request.SellingPrice);

        var dto = new RecipeCostResultDto(
            request.RecipeId,
            result.IngredientCost,
            result.CostPerYieldUnit,
            result.SellingPrice,
            result.FoodCostPercentage,
            request.CostingDate ?? DateTimeOffset.UtcNow,
            result.Ingredients.Select(x =>
                new CostIngredientResultDto(
                    x.IngredientId,
                    x.RecipeQuantity,
                    x.BaseQuantity,
                    x.WasteAdjustedQuantity,
                    x.UnitCost,
                    x.ExtendedCost)).ToArray());

        return Task.FromResult(dto);
    }
}
