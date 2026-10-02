using GastronomIQ.Application.Recipes;

namespace GastronomIQ.Api.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes");

        group.MapPost("/", async (
            CreateRecipeRequest request,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var recipe = await service.CreateAsync(request, ct);
            return Results.Created($"/api/v1/recipes/{recipe.Id}", recipe);
        });

        group.MapPost("/versions", async (
            CreateRecipeVersionRequest request,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var version = await service.CreateVersionAsync(request, ct);
            return Results.Created(
                $"/api/v1/recipe-versions/{version.Id}",
                version);
        });

        group.MapPost("/versions/{versionId:guid}/ingredients", async (
            Guid versionId,
            AddRecipeIngredientRequest request,
            IRecipeService service,
            CancellationToken ct) =>
        {
            if (versionId != request.RecipeVersionId)
                return Results.BadRequest(new { error = "Version ID mismatch." });

            await service.AddIngredientAsync(request, ct);
            return Results.NoContent();
        });

        group.MapPost("/versions/{versionId:guid}/publish", async (
            Guid versionId,
            IRecipeService service,
            CancellationToken ct) =>
        {
            await service.PublishAsync(versionId, ct);
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            Guid organizationId,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var recipe = await service.GetAsync(
                organizationId,
                id,
                ct);

            return recipe is null
                ? Results.NotFound()
                : Results.Ok(recipe);
        });

        group.MapGet("/", async (
            Guid organizationId,
            IRecipeService service,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

            var recipes = await service.SearchAsync(
                organizationId,
                search,
                page,
                pageSize,
                ct);

            return Results.Ok(new
            {
                items = recipes,
                page,
                pageSize
            });
        });
    }
}
