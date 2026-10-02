using GastronomIQ.Api.Security;
using GastronomIQ.Application.Culinary;

namespace GastronomIQ.Api.Endpoints;

public static class IngredientEndpoints
{
    public static void MapIngredientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ingredients")
            .WithTags("Ingredients");

        group.MapPost("/", async (
            CreateIngredientRequest request,
            IIngredientService service,
            CancellationToken ct) =>
        {
            var item = await service.CreateAsync(request, ct);
            return Results.Created($"/api/v1/ingredients/{item.Id}", item);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext http,
            IIngredientService service,
            CancellationToken ct) =>
        {
            var org = http.User.GetOrganizationId();
            if (org == Guid.Empty)
                org = http.Request.Headers.TryGetValue("X-Organization-Id", out var header)
                    && Guid.TryParse(header, out var parsed)
                        ? parsed
                        : Guid.Empty;

            if (org == Guid.Empty)
                return Results.BadRequest(new { error = "Organization context is required." });

            var item = await service.GetAsync(org, id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapGet("/", async (
            HttpContext http,
            IIngredientService service,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            var org = http.User.GetOrganizationId();

            if (org == Guid.Empty &&
                http.Request.Headers.TryGetValue("X-Organization-Id", out var header))
                Guid.TryParse(header, out org);

            if (org == Guid.Empty)
                return Results.BadRequest(new { error = "Organization context is required." });

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

            var items = await service.SearchAsync(org, search, page, pageSize, ct);
            return Results.Ok(new { items, page, pageSize });
        });
    }
}
