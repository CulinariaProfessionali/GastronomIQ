namespace GastronomIQ.Domain.Culinary;

public sealed record IngredientCost(
    Guid IngredientId,
    decimal UnitCost,
    string Currency,
    DateTimeOffset EffectiveAt);

public static class CostCalculator
{
    public static decimal CalculateIngredientCost(
        decimal quantityInBaseUnits,
        decimal unitCost) =>
        decimal.Round(quantityInBaseUnits * unitCost, 4, MidpointRounding.AwayFromZero);
}
