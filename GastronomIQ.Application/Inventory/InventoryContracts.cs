namespace GastronomIQ.Application.Inventory;

public sealed record CreateStockLocationRequest(
    Guid OrganizationId,
    Guid BranchId,
    string Name,
    string Code);

public sealed record StockLocationDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string Name,
    string Code);

public sealed record PostInventoryMovementRequest(
    Guid OrganizationId,
    Guid BranchId,
    Guid StockLocationId,
    Guid IngredientId,
    string MovementType,
    decimal Quantity,
    decimal UnitCost,
    string Currency,
    Guid? ReferenceId);

public sealed record InventoryMovementDto(
    Guid Id,
    Guid StockLocationId,
    Guid IngredientId,
    string MovementType,
    decimal Quantity,
    decimal SignedQuantity,
    decimal UnitCost,
    string Currency,
    DateTimeOffset OccurredAt);

public sealed record InventoryBalanceDto(
    Guid StockLocationId,
    Guid IngredientId,
    decimal QuantityOnHand,
    decimal StockValue,
    string Currency);

public interface IInventoryService
{
    Task<StockLocationDto> CreateLocationAsync(
        CreateStockLocationRequest request,
        CancellationToken cancellationToken);

    Task<InventoryMovementDto> PostMovementAsync(
        PostInventoryMovementRequest request,
        CancellationToken cancellationToken);

    Task<InventoryBalanceDto> GetBalanceAsync(
        Guid organizationId,
        Guid stockLocationId,
        Guid ingredientId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryMovementDto>> GetLedgerAsync(
        Guid organizationId,
        Guid stockLocationId,
        Guid ingredientId,
        CancellationToken cancellationToken);
}
