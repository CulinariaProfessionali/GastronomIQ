using System.Collections.Concurrent;
using GastronomIQ.Application.Recipes;
using GastronomIQ.Domain.Recipes;

namespace GastronomIQ.Infrastructure.Recipes;

public sealed class InMemoryRecipeService : IRecipeService
{
    private readonly ConcurrentDictionary<Guid, Recipe> _recipes = new();
    private readonly ConcurrentDictionary<Guid, Guid> _versionToRecipe = new();

    public Task<RecipeDto> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = new Recipe(request.OrganizationId, request.Name, request.Code);
        _recipes[recipe.Id.Value] = recipe;

        return Task.FromResult(Map(recipe));
    }

    public Task<RecipeVersionDto> CreateVersionAsync(
        CreateRecipeVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!_recipes.TryGetValue(request.RecipeId, out var recipe))
            throw new KeyNotFoundException("Recipe not found.");

        var version = recipe.CreateDraft(
            request.YieldQuantity,
            request.YieldUnitId,
            request.Method);

        _versionToRecipe[version.Id.Value.Value] = recipe.Id.Value;

        return Task.FromResult(Map(version));
    }

    public Task AddIngredientAsync(
        AddRecipeIngredientRequest request,
        CancellationToken cancellationToken)
    {
        if (!_versionToRecipe.TryGetValue(request.RecipeVersionId, out var recipeId) ||
            !_recipes.TryGetValue(recipeId, out var recipe))
            throw new KeyNotFoundException("Recipe version not found.");

        var version = recipe.Versions.First(x =>
            x.Id.Value == request.RecipeVersionId);

        version.AddIngredient(new RecipeIngredientLine(
            request.IngredientId,
            request.Quantity,
            request.UnitId,
            request.WastePercentage));

        return Task.CompletedTask;
    }

    public Task PublishAsync(
        Guid recipeVersionId,
        CancellationToken cancellationToken)
    {
        if (!_versionToRecipe.TryGetValue(recipeVersionId, out var recipeId) ||
            !_recipes.TryGetValue(recipeId, out var recipe))
            throw new KeyNotFoundException("Recipe version not found.");

        var version = recipe.Versions.First(x => x.Id.Value == recipeVersionId);
        version.Publish();

        return Task.CompletedTask;
    }

    public Task<RecipeDto?> GetAsync(
        Guid organizationId,
        Guid recipeId,
        CancellationToken cancellationToken)
    {
        if (!_recipes.TryGetValue(recipeId, out var recipe) ||
            recipe.OrganizationId != organizationId)
            return Task.FromResult<RecipeDto?>(null);

        return Task.FromResult<RecipeDto?>(Map(recipe));
    }

    public Task<IReadOnlyList<RecipeDto>> SearchAsync(
        Guid organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _recipes.Values
            .Where(r => r.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Code.Contains(search, StringComparison.OrdinalIgnoreCase));

        var result = query
            .OrderBy(r => r.Name)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(Math.Min(pageSize, 100))
            .Select(Map)
            .ToArray();

        return Task.FromResult<IReadOnlyList<RecipeDto>>(result);
    }

    private static RecipeDto Map(Recipe recipe) =>
        new(
            recipe.Id.Value,
            recipe.OrganizationId,
            recipe.Name,
            recipe.Code,
            recipe.Versions.Select(Map).ToArray());

    private static RecipeVersionDto Map(RecipeVersion version) =>
        new(
            version.Id.Value,
            version.RecipeId.Value,
            version.VersionNumber,
            version.Status.ToString().ToUpperInvariant(),
            version.YieldQuantity,
            version.YieldUnitId,
            version.Method,
            version.Ingredients
                .Select(x => new RecipeIngredientDto(
                    x.IngredientId,
                    x.Quantity,
                    x.UnitId,
                    x.WastePercentage))
                .ToArray());
}
