namespace GastronomIQ.Application.MenuEngineering;

public sealed record CreateMenuItemRequest(
    Guid OrganizationId,
    Guid BranchId,
    Guid RecipeId,
    Guid RecipeVersionId,
    string Name,
    string Code);

public sealed record SetMenuPriceRequest(
    Guid MenuItemId,
    decimal SellingPrice,
    string Currency);

public sealed record MenuItemDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid RecipeId,
    Guid RecipeVersionId,
    string Name,
    string Code,
    decimal? CurrentSellingPrice,
    string? Currency);

public sealed record MenuPerformanceInputRequest(
    Guid MenuItemId,
    string Name,
    decimal SellingPrice,
    decimal RecipeCost,
    decimal QuantitySold);

public sealed record MenuEngineeringRequest(
    Guid BranchId,
    IReadOnlyList<MenuPerformanceInputRequest> Items);

public sealed record MenuEngineeringResultDto(
    Guid MenuItemId,
    string Name,
    decimal SellingPrice,
    decimal RecipeCost,
    decimal QuantitySold,
    decimal ContributionMargin,
    decimal FoodCostPercentage,
    string Classification);

public interface IMenuEngineeringService
{
    Task<MenuItemDto> CreateAsync(
        CreateMenuItemRequest request,
        CancellationToken cancellationToken);

    Task SetPriceAsync(
        SetMenuPriceRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuEngineeringResultDto>> AnalyzeAsync(
        MenuEngineeringRequest request,
        CancellationToken cancellationToken);
}
