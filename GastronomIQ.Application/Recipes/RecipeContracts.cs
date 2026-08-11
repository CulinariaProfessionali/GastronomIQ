namespace GastronomIQ.Application.Recipes;

public sealed record CreateRecipeRequest(
    Guid OrganizationId,
    string Name,
    string Code);

public sealed record CreateRecipeVersionRequest(
    Guid RecipeId,
    decimal YieldQuantity,
    Guid YieldUnitId,
    string? Method);

public sealed record AddRecipeIngredientRequest(
    Guid RecipeVersionId,
    Guid IngredientId,
    decimal Quantity,
    Guid UnitId,
    decimal? WastePercentage);

public sealed record RecipeIngredientDto(
    Guid IngredientId,
    decimal Quantity,
    Guid UnitId,
    decimal? WastePercentage);

public sealed record RecipeVersionDto(
    Guid Id,
    Guid RecipeId,
    int VersionNumber,
    string Status,
    decimal YieldQuantity,
    Guid YieldUnitId,
    string? Method,
    IReadOnlyList<RecipeIngredientDto> Ingredients);

public sealed record RecipeDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Code,
    IReadOnlyList<RecipeVersionDto> Versions);

public interface IRecipeService
{
    Task<RecipeDto> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken);

    Task<RecipeVersionDto> CreateVersionAsync(
        CreateRecipeVersionRequest request,
        CancellationToken cancellationToken);

    Task AddIngredientAsync(
        AddRecipeIngredientRequest request,
        CancellationToken cancellationToken);

    Task PublishAsync(
        Guid recipeVersionId,
        CancellationToken cancellationToken);

    Task<RecipeDto?> GetAsync(
        Guid organizationId,
        Guid recipeId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipeDto>> SearchAsync(
        Guid organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
