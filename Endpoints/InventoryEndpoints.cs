using GastronomIQ.Application.Inventory;

namespace GastronomIQ.Api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var locations = app.MapGroup("/api/v1/inventory/locations")
            .WithTags("Inventory");

        locations.MapPost("/", async (
            CreateStockLocationRequest request,
            IInventoryService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateLocationAsync(request, ct);
            return Results.Created(
                $"/api/v1/inventory/locations/{result.Id}",
                result);
        });

        var ledger = app.MapGroup("/api/v1/inventory")
            .WithTags("Inventory");

        ledger.MapPost("/movements", async (
            PostInventoryMovementRequest request,
            IInventoryService service,
            CancellationToken ct) =>
        {
            var result = await service.PostMovementAsync(request, ct);
            return Results.Created(
                $"/api/v1/inventory/movements/{result.Id}",
                result);
        });

        ledger.MapGet("/balance", async (
            Guid organizationId,
            Guid stockLocationId,
            Guid ingredientId,
            IInventoryService service,
            CancellationToken ct) =>
        {
            var result = await service.GetBalanceAsync(
                organizationId,
                stockLocationId,
                ingredientId,
                ct);

            return Results.Ok(result);
        });

        ledger.MapGet("/ledger", async (
            Guid organizationId,
            Guid stockLocationId,
            Guid ingredientId,
            IInventoryService service,
            CancellationToken ct) =>
        {
            var result = await service.GetLedgerAsync(
                organizationId,
                stockLocationId,
                ingredientId,
                ct);

            return Results.Ok(new { items = result });
        });
    }
}
