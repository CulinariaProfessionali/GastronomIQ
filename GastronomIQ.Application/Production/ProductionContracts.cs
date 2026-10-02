namespace GastronomIQ.Application.Production;

public sealed record CreateProductionBatchRequest(
    Guid OrganizationId,
    Guid BranchId,
    Guid RecipeId,
    Guid RecipeVersionId,
    string Number,
    decimal PlannedYield,
    Guid YieldUnitId);

public sealed record AddProductionLineRequest(
    Guid ProductionBatchId,
    Guid IngredientId,
    decimal PlannedQuantity,
    Guid UnitId,
    decimal UnitCost);

public sealed record RecordProductionLineRequest(
    Guid ProductionBatchId,
    Guid IngredientId,
    decimal ActualQuantity);

public sealed record CompleteProductionRequest(
    Guid ProductionBatchId,
    decimal ActualYield,
    decimal WasteQuantity,
    Guid StockLocationId);

public sealed record ProductionBatchLineDto(
    Guid Id,
    Guid IngredientId,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    decimal CostVariance,
    Guid UnitId,
    decimal UnitCost);

public sealed record ProductionBatchDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid RecipeId,
    Guid RecipeVersionId,
    string Number,
    string Status,
    decimal PlannedYield,
    decimal ActualYield,
    decimal WasteQuantity,
    decimal PlannedCost,
    decimal ActualCost,
    decimal CostVariance,
    IReadOnlyList<ProductionBatchLineDto> Lines);

public interface IProductionService
{
    Task<ProductionBatchDto> CreateAsync(
        CreateProductionBatchRequest request,
        CancellationToken cancellationToken);

    Task AddLineAsync(
        AddProductionLineRequest request,
        CancellationToken cancellationToken);

    Task StartAsync(
        Guid productionBatchId,
        CancellationToken cancellationToken);

    Task RecordLineAsync(
        RecordProductionLineRequest request,
        CancellationToken cancellationToken);

    Task<ProductionBatchDto> CompleteAsync(
        CompleteProductionRequest request,
        CancellationToken cancellationToken);

    Task<ProductionBatchDto?> GetAsync(
        Guid organizationId,
        Guid productionBatchId,
        CancellationToken cancellationToken);
}
