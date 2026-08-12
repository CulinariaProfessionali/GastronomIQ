using GastronomIQ.Api.Security;
using GastronomIQ.Application.Authorization;
using GastronomIQ.Application.Recipes;

namespace GastronomIQ.Api.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .RequireAuthorization()
            .WithTags("Recipes");

        group.MapPost("/", async (
            CreateRecipeRequest request,
            HttpContext http,
            IAuthorizationService authorization,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.RecipeManage,
                out var context);
            if (authResult is not null)
                return authResult;

            var effectiveOrganizationId = request.OrganizationId == Guid.Empty
                ? context.OrganizationId
                : request.OrganizationId;

            if (!authorization.BelongsToOrganization(context, effectiveOrganizationId))
                return Results.Forbid();

            var scopedRequest = request with { OrganizationId = effectiveOrganizationId };
            var recipe = await service.CreateAsync(scopedRequest, ct);
            return Results.Created($"/api/v1/recipes/{recipe.Id}", recipe);
        });

        group.MapPost("/versions", async (
            CreateRecipeVersionRequest request,
            HttpContext http,
            IAuthorizationService authorization,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.RecipeManage,
                out var context);
            if (authResult is not null)
                return authResult;

            var version = await service.CreateVersionAsync(
                context.OrganizationId,
                request,
                ct);
            return Results.Created(
                $"/api/v1/recipe-versions/{version.Id}",
                version);
        });

        group.MapPost("/versions/{versionId:guid}/ingredients", async (
            Guid versionId,
            AddRecipeIngredientRequest request,
            HttpContext http,
            IAuthorizationService authorization,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.RecipeManage,
                out var context);
            if (authResult is not null)
                return authResult;

            if (versionId != request.RecipeVersionId)
                return Results.BadRequest(new { error = "Version ID mismatch." });

            await service.AddIngredientAsync(
                context.OrganizationId,
                request,
                ct);
            return Results.NoContent();
        });

        group.MapPost("/versions/{versionId:guid}/publish", async (
            Guid versionId,
            HttpContext http,
            IAuthorizationService authorization,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.RecipeManage,
                out var context);
            if (authResult is not null)
                return authResult;

            await service.PublishAsync(
                context.OrganizationId,
                versionId,
                ct);
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            Guid organizationId,
            HttpContext http,
            IAuthorizationService authorization,
            IRecipeService service,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.RecipeRead,
                out var context);
            if (authResult is not null)
                return authResult;

            var scopedOrganizationId = organizationId == Guid.Empty
                ? context.OrganizationId
                : organizationId;

            var scopeResult = authorization.RequireOrganizationScope(context, scopedOrganizationId);
            if (scopeResult is not null)
                return scopeResult;

            var recipe = await service.GetAsync(
                scopedOrganizationId,
                id,
                ct);

            return recipe is null
                ? Results.NotFound()
                : Results.Ok(recipe);
        });

        group.MapGet("/", async (
            Guid organizationId,
            HttpContext http,
            IAuthorizationService authorization,
            IRecipeService service,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.RecipeRead,
                out var context);
            if (authResult is not null)
                return authResult;

            var scopedOrganizationId = organizationId == Guid.Empty
                ? context.OrganizationId
                : organizationId;

            var scopeResult = authorization.RequireOrganizationScope(context, scopedOrganizationId);
            if (scopeResult is not null)
                return scopeResult;

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

            var recipes = await service.SearchAsync(
                scopedOrganizationId,
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
