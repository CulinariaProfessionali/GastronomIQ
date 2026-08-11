namespace GastronomIQ.Application.Costing;

public sealed record CostRecipeIngredientRequest(
    Guid IngredientId,
    decimal RecipeQuantity,
    decimal UnitConversionToBase,
    decimal UnitCost,
    decimal WastePercentage);

public sealed record CostRecipeRequest(
    Guid RecipeId,
    decimal YieldQuantity,
    decimal? SellingPrice,
    DateTimeOffset? CostingDate,
    IReadOnlyList<CostRecipeIngredientRequest> Ingredients);

public sealed record CostIngredientResultDto(
    Guid IngredientId,
    decimal RecipeQuantity,
    decimal BaseQuantity,
    decimal WasteAdjustedQuantity,
    decimal UnitCost,
    decimal ExtendedCost);

public sealed record RecipeCostResultDto(
    Guid RecipeId,
    decimal IngredientCost,
    decimal CostPerYieldUnit,
    decimal? SellingPrice,
    decimal? FoodCostPercentage,
    DateTimeOffset CostingDate,
    IReadOnlyList<CostIngredientResultDto> Ingredients);

public interface IRecipeCostingService
{
    Task<RecipeCostResultDto> CalculateAsync(
        CostRecipeRequest request,
        CancellationToken cancellationToken);
}
