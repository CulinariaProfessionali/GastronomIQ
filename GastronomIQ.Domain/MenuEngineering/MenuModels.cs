namespace GastronomIQ.Domain.MenuEngineering;

public enum MenuEngineeringClass
{
    Star,
    Plowhorse,
    Puzzle,
    Dog
}

public sealed record MenuItemId(Guid Value);

public sealed class MenuItem
{
    private readonly Dictionary<Guid, MenuItemPrice> _prices = new();

    public MenuItemId Id { get; init; } = new(Guid.NewGuid());
    public Guid OrganizationId { get; init; }
    public Guid BranchId { get; init; }
    public Guid RecipeId { get; init; }
    public Guid RecipeVersionId { get; init; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<MenuItemPrice> Prices =>
        _prices.Values.ToArray();

    public MenuItem(
        Guid organizationId,
        Guid branchId,
        Guid recipeId,
        Guid recipeVersionId,
        string name,
        string code)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (branchId == Guid.Empty) throw new ArgumentException("Branch is required.");
        if (recipeId == Guid.Empty) throw new ArgumentException("Recipe is required.");
        if (recipeVersionId == Guid.Empty) throw new ArgumentException("Recipe version is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Menu item name is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Menu item code is required.");

        OrganizationId = organizationId;
        BranchId = branchId;
        RecipeId = recipeId;
        RecipeVersionId = recipeVersionId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
    }

    public void SetPrice(decimal sellingPrice, string currency = "INR")
    {
        if (sellingPrice < 0) throw new ArgumentOutOfRangeException(nameof(sellingPrice));

        _prices[Guid.NewGuid()] = new MenuItemPrice(
            sellingPrice,
            currency.ToUpperInvariant(),
            DateTimeOffset.UtcNow);
    }

    public MenuItemPrice? CurrentPrice =>
        _prices.Values
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefault();
}

public sealed record MenuItemPrice(
    decimal SellingPrice,
    string Currency,
    DateTimeOffset EffectiveFrom);

public sealed record MenuPerformanceInput(
    Guid MenuItemId,
    string Name,
    decimal SellingPrice,
    decimal RecipeCost,
    decimal QuantitySold);

public sealed record MenuPerformanceResult(
    Guid MenuItemId,
    string Name,
    decimal SellingPrice,
    decimal RecipeCost,
    decimal QuantitySold,
    decimal ContributionMargin,
    decimal FoodCostPercentage,
    MenuEngineeringClass Classification);

public static class MenuEngineeringCalculator
{
    public static IReadOnlyList<MenuPerformanceResult> Classify(
        IEnumerable<MenuPerformanceInput> inputs)
    {
        var items = inputs.ToArray();

        if (items.Length == 0)
            return Array.Empty<MenuPerformanceResult>();

        var averageQuantity = items.Average(x => x.QuantitySold);

        var preliminary = items.Select(x =>
        {
            var margin = x.SellingPrice - x.RecipeCost;
            var foodCost = x.SellingPrice > 0
                ? decimal.Round(x.RecipeCost / x.SellingPrice * 100m, 2)
                : 0m;

            return new
            {
                x.MenuItemId,
                x.Name,
                x.SellingPrice,
                x.RecipeCost,
                x.QuantitySold,
                ContributionMargin = decimal.Round(margin, 4),
                FoodCostPercentage = foodCost
            };
        }).ToArray();

        var averageMargin = preliminary.Average(x => x.ContributionMargin);

        return preliminary.Select(x =>
        {
            var highPopularity = x.QuantitySold >= averageQuantity;
            var highMargin = x.ContributionMargin >= averageMargin;

            var classification =
                (highPopularity, highMargin) switch
                {
                    (true, true) => MenuEngineeringClass.Star,
                    (true, false) => MenuEngineeringClass.Plowhorse,
                    (false, true) => MenuEngineeringClass.Puzzle,
                    _ => MenuEngineeringClass.Dog
                };

            return new MenuPerformanceResult(
                x.MenuItemId,
                x.Name,
                x.SellingPrice,
                x.RecipeCost,
                x.QuantitySold,
                x.ContributionMargin,
                x.FoodCostPercentage,
                classification);
        }).ToArray();
    }
}
