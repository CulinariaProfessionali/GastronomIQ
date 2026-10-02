using System.Collections.Concurrent;
using GastronomIQ.Application.MenuEngineering;
using GastronomIQ.Domain.MenuEngineering;

namespace GastronomIQ.Infrastructure.MenuEngineering;

public sealed class InMemoryMenuEngineeringService : IMenuEngineeringService
{
    private readonly ConcurrentDictionary<Guid, MenuItem> _items = new();

    public Task<MenuItemDto> CreateAsync(
        CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = new MenuItem(
            request.OrganizationId,
            request.BranchId,
            request.RecipeId,
            request.RecipeVersionId,
            request.Name,
            request.Code);

        _items[item.Id.Value] = item;

        return Task.FromResult(Map(item));
    }

    public Task SetPriceAsync(
        SetMenuPriceRequest request,
        CancellationToken cancellationToken)
    {
        if (!_items.TryGetValue(request.MenuItemId, out var item))
            throw new KeyNotFoundException("Menu item not found.");

        item.SetPrice(request.SellingPrice, request.Currency);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MenuEngineeringResultDto>> AnalyzeAsync(
        MenuEngineeringRequest request,
        CancellationToken cancellationToken)
    {
        var inputs = request.Items.Select(x =>
            new MenuPerformanceInput(
                x.MenuItemId,
                x.Name,
                x.SellingPrice,
                x.RecipeCost,
                x.QuantitySold));

        var results = MenuEngineeringCalculator.Classify(inputs)
            .Select(x => new MenuEngineeringResultDto(
                x.MenuItemId,
                x.Name,
                x.SellingPrice,
                x.RecipeCost,
                x.QuantitySold,
                x.ContributionMargin,
                x.FoodCostPercentage,
                x.Classification.ToString().ToUpperInvariant()))
            .ToArray();

        return Task.FromResult<IReadOnlyList<MenuEngineeringResultDto>>(results);
    }

    private static MenuItemDto Map(MenuItem item)
    {
        var price = item.CurrentPrice;

        return new MenuItemDto(
            item.Id.Value,
            item.OrganizationId,
            item.BranchId,
            item.RecipeId,
            item.RecipeVersionId,
            item.Name,
            item.Code,
            price?.SellingPrice,
            price?.Currency);
    }
}
