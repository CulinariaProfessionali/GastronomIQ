using GastronomIQ.Application.Costing;

namespace GastronomIQ.Api.Endpoints;

public static class CostingEndpoints
{
    public static void MapCostingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/costing")
            .WithTags("Recipe Costing");

        group.MapPost("/recipes/calculate", async (
            CostRecipeRequest request,
            IRecipeCostingService service,
            CancellationToken ct) =>
        {
            var result = await service.CalculateAsync(request, ct);
            return Results.Ok(result);
        });
    }
}
