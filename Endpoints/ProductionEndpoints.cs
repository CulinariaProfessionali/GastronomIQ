using GastronomIQ.Application.Production;

namespace GastronomIQ.Api.Endpoints;

public static class ProductionEndpoints
{
    public static void MapProductionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/production/batches")
            .WithTags("Production");

        group.MapPost("/", async (
            CreateProductionBatchRequest request,
            IProductionService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return Results.Created(
                $"/api/v1/production/batches/{result.Id}",
                result);
        });

        group.MapPost("/{id:guid}/lines", async (
            Guid id,
            AddProductionLineRequest request,
            IProductionService service,
            CancellationToken ct) =>
        {
            if (id != request.ProductionBatchId)
                return Results.BadRequest(new { error = "Batch ID mismatch." });

            await service.AddLineAsync(request, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/start", async (
            Guid id,
            IProductionService service,
            CancellationToken ct) =>
        {
            await service.StartAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/lines/actual", async (
            Guid id,
            RecordProductionLineRequest request,
            IProductionService service,
            CancellationToken ct) =>
        {
            if (id != request.ProductionBatchId)
                return Results.BadRequest(new { error = "Batch ID mismatch." });

            await service.RecordLineAsync(request, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            CompleteProductionRequest request,
            IProductionService service,
            CancellationToken ct) =>
        {
            if (id != request.ProductionBatchId)
                return Results.BadRequest(new { error = "Batch ID mismatch." });

            var result = await service.CompleteAsync(request, ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            Guid organizationId,
            IProductionService service,
            CancellationToken ct) =>
        {
            var result = await service.GetAsync(
                organizationId,
                id,
                ct);

            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}
