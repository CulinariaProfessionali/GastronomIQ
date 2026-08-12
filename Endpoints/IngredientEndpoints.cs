using GastronomIQ.Api.Security;
using GastronomIQ.Application.Authorization;
using GastronomIQ.Application.Culinary;

namespace GastronomIQ.Api.Endpoints;

public static class IngredientEndpoints
{
    public static void MapIngredientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ingredients")
            .RequireAuthorization()
            .WithTags("Ingredients");

        group.MapPost("/", async (
            CreateIngredientRequest request,
            HttpContext http,
            IAuthorizationService authorization,
            IIngredientService service,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.IngredientManage,
                out var context);
            if (authResult is not null)
                return authResult;

            var effectiveOrganizationId = request.OrganizationId == Guid.Empty
                ? context.OrganizationId
                : request.OrganizationId;

            var scopeResult = authorization.RequireOrganizationScope(context, effectiveOrganizationId);
            if (scopeResult is not null)
                return scopeResult;

            var scopedRequest = request with { OrganizationId = effectiveOrganizationId };
            var item = await service.CreateAsync(scopedRequest, ct);
            return Results.Created($"/api/v1/ingredients/{item.Id}", item);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext http,
            IAuthorizationService authorization,
            IIngredientService service,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.IngredientRead,
                out var context);
            if (authResult is not null)
                return authResult;

            var org = context.OrganizationId;

            var item = await service.GetAsync(org, id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapGet("/", async (
            HttpContext http,
            IAuthorizationService authorization,
            IIngredientService service,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            var authResult = http.RequirePermission(
                authorization,
                PermissionCatalogue.IngredientRead,
                out var context);
            if (authResult is not null)
                return authResult;

            var org = context.OrganizationId;

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

            var items = await service.SearchAsync(org, search, page, pageSize, ct);
            return Results.Ok(new { items, page, pageSize });
        });
    }
}
