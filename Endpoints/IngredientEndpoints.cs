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
            var context = new AuthorizationContext(
                http.User.GetUserId(),
                http.User.GetOrganizationId(),
                http.User.GetPermissions());

            if (context.UserId == Guid.Empty || context.OrganizationId == Guid.Empty)
                return Results.Unauthorized();

            if (!authorization.HasPermission(context, PermissionCatalogue.IngredientManage))
                return Results.Forbid();

            var effectiveOrganizationId = request.OrganizationId == Guid.Empty
                ? context.OrganizationId
                : request.OrganizationId;

            if (!authorization.BelongsToOrganization(context, effectiveOrganizationId))
                return Results.Forbid();

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
            var context = new AuthorizationContext(
                http.User.GetUserId(),
                http.User.GetOrganizationId(),
                http.User.GetPermissions());

            if (context.UserId == Guid.Empty || context.OrganizationId == Guid.Empty)
                return Results.Unauthorized();

            if (!authorization.HasPermission(context, PermissionCatalogue.IngredientRead))
                return Results.Forbid();

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
            var context = new AuthorizationContext(
                http.User.GetUserId(),
                http.User.GetOrganizationId(),
                http.User.GetPermissions());

            if (context.UserId == Guid.Empty || context.OrganizationId == Guid.Empty)
                return Results.Unauthorized();

            if (!authorization.HasPermission(context, PermissionCatalogue.IngredientRead))
                return Results.Forbid();

            var org = context.OrganizationId;

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

            var items = await service.SearchAsync(org, search, page, pageSize, ct);
            return Results.Ok(new { items, page, pageSize });
        });
    }
}
