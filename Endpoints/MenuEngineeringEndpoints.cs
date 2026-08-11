using GastronomIQ.Application.MenuEngineering;

namespace GastronomIQ.Api.Endpoints;

public static class MenuEngineeringEndpoints
{
    public static void MapMenuEngineeringEndpoints(this WebApplication app)
    {
        var items = app.MapGroup("/api/v1/menu")
            .WithTags("Menu Engineering");

        items.MapPost("/items", async (
            CreateMenuItemRequest request,
            IMenuEngineeringService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return Results.Created(
                $"/api/v1/menu/items/{result.Id}",
                result);
        });

        items.MapPost("/items/price", async (
            SetMenuPriceRequest request,
            IMenuEngineeringService service,
            CancellationToken ct) =>
        {
            await service.SetPriceAsync(request, ct);
            return Results.NoContent();
        });

        items.MapPost("/engineering", async (
            MenuEngineeringRequest request,
            IMenuEngineeringService service,
            CancellationToken ct) =>
        {
            var result = await service.AnalyzeAsync(request, ct);
            return Results.Ok(new { items = result });
        });
    }
}
