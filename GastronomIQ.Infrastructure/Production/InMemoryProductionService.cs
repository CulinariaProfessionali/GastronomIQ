using System.Collections.Concurrent;
using GastronomIQ.Application.Inventory;
using GastronomIQ.Application.Production;
using GastronomIQ.Domain.Production;

namespace GastronomIQ.Infrastructure.Production;

public sealed class InMemoryProductionService : IProductionService
{
    private readonly ConcurrentDictionary<Guid, ProductionBatch> _batches = new();
    private readonly IInventoryService _inventory;

    public InMemoryProductionService(IInventoryService inventory) =>
        _inventory = inventory;

    public Task<ProductionBatchDto> CreateAsync(
        CreateProductionBatchRequest request,
        CancellationToken cancellationToken)
    {
        var batch = new ProductionBatch(
            request.OrganizationId,
            request.BranchId,
            request.RecipeId,
            request.RecipeVersionId,
            request.Number,
            request.PlannedYield,
            request.YieldUnitId);

        _batches[batch.Id.Value] = batch;
        return Task.FromResult(Map(batch));
    }

    public Task AddLineAsync(
        AddProductionLineRequest request,
        CancellationToken cancellationToken)
    {
        var batch = Get(request.ProductionBatchId);

        batch.AddLine(new ProductionBatchLine(
            request.IngredientId,
            request.PlannedQuantity,
            request.UnitId,
            request.UnitCost));

        return Task.CompletedTask;
    }

    public Task StartAsync(
        Guid productionBatchId,
        CancellationToken cancellationToken)
    {
        Get(productionBatchId).Start();
        return Task.CompletedTask;
    }

    public Task RecordLineAsync(
        RecordProductionLineRequest request,
        CancellationToken cancellationToken)
    {
        Get(request.ProductionBatchId)
            .RecordActualIngredientQuantity(
                request.IngredientId,
                request.ActualQuantity);

        return Task.CompletedTask;
    }

    public async Task<ProductionBatchDto> CompleteAsync(
        CompleteProductionRequest request,
        CancellationToken cancellationToken)
    {
        var batch = Get(request.ProductionBatchId);

        batch.RecordActualYield(request.ActualYield);
        batch.RecordWaste(request.WasteQuantity);

        foreach (var line in batch.Lines)
        {
            if (line.ActualQuantity <= 0)
                line.RecordActual(line.PlannedQuantity);

            if (line.ActualQuantity > 0)
            {
                await _inventory.PostMovementAsync(
                    new PostInventoryMovementRequest(
                        batch.OrganizationId,
                        batch.BranchId,
                        request.StockLocationId,
                        line.IngredientId,
                        "Consumption",
                        line.ActualQuantity,
                        line.UnitCost,
                        "INR",
                        batch.Id.Value),
                    cancellationToken);
            }
        }

        batch.Complete();
        return Map(batch);
    }

    public Task<ProductionBatchDto?> GetAsync(
        Guid organizationId,
        Guid productionBatchId,
        CancellationToken cancellationToken)
    {
        if (!_batches.TryGetValue(productionBatchId, out var batch) ||
            batch.OrganizationId != organizationId)
            return Task.FromResult<ProductionBatchDto?>(null);

        return Task.FromResult<ProductionBatchDto?>(Map(batch));
    }

    private ProductionBatch Get(Guid id) =>
        _batches.TryGetValue(id, out var batch)
            ? batch
            : throw new KeyNotFoundException("Production batch not found.");

    private static ProductionBatchDto Map(ProductionBatch batch) =>
        new(
            batch.Id.Value,
            batch.OrganizationId,
            batch.BranchId,
            batch.RecipeId,
            batch.RecipeVersionId,
            batch.Number,
            batch.Status.ToString().ToUpperInvariant(),
            batch.PlannedYield,
            batch.ActualYield,
            batch.WasteQuantity,
            batch.PlannedCost,
            batch.ActualCost,
            batch.CostVariance,
            batch.Lines.Select(x => new ProductionBatchLineDto(
                x.Id.Value,
                x.IngredientId,
                x.PlannedQuantity,
                x.ActualQuantity,
                x.CostVariance,
                x.UnitId,
                x.UnitCost)).ToArray());
}
