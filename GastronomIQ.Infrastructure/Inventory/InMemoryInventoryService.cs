using System.Collections.Concurrent;
using GastronomIQ.Application.Inventory;
using GastronomIQ.Domain.Inventory;

namespace GastronomIQ.Infrastructure.Inventory;

public sealed class InMemoryInventoryService : IInventoryService
{
    private readonly ConcurrentDictionary<Guid, StockLocationDto> _locations = new();
    private readonly ConcurrentBag<InventoryMovement> _movements = new();

    public Task<StockLocationDto> CreateLocationAsync(
        CreateStockLocationRequest request,
        CancellationToken cancellationToken)
    {
        var location = new StockLocationDto(
            Guid.NewGuid(),
            request.OrganizationId,
            request.BranchId,
            request.Name.Trim(),
            request.Code.Trim().ToUpperInvariant());

        _locations[location.Id] = location;
        return Task.FromResult(location);
    }

    public Task<InventoryMovementDto> PostMovementAsync(
        PostInventoryMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<InventoryMovementType>(
                request.MovementType,
                true,
                out var type))
            throw new ArgumentException("Invalid movement type.");

        var movement = new InventoryMovement(
            new InventoryMovementId(Guid.NewGuid()),
            request.OrganizationId,
            request.BranchId,
            request.StockLocationId,
            request.IngredientId,
            type,
            request.Quantity,
            request.UnitCost,
            request.Currency.ToUpperInvariant(),
            request.ReferenceId,
            DateTimeOffset.UtcNow);

        if (type is InventoryMovementType.TransferOut
            or InventoryMovementType.Waste
            or InventoryMovementType.Consumption)
        {
            var balance = GetRawBalance(
                request.OrganizationId,
                request.StockLocationId,
                request.IngredientId);

            if (balance < request.Quantity)
                throw new InvalidOperationException("Insufficient stock.");
        }

        _movements.Add(movement);

        return Task.FromResult(new InventoryMovementDto(
            movement.Id.Value,
            movement.StockLocationId.Value,
            movement.IngredientId,
            movement.Type.ToString().ToUpperInvariant(),
            movement.Quantity,
            movement.SignedQuantity,
            movement.UnitCost,
            movement.Currency,
            movement.OccurredAt));
    }

    public Task<InventoryBalanceDto> GetBalanceAsync(
        Guid organizationId,
        Guid stockLocationId,
        Guid ingredientId,
        CancellationToken cancellationToken)
    {
        var movements = _movements.Where(x =>
            x.OrganizationId == organizationId &&
            x.StockLocationId.Value == stockLocationId &&
            x.IngredientId == ingredientId);

        var quantity = InventoryCalculator.QuantityOnHand(movements);
        var value = InventoryCalculator.StockValue(movements);
        var currency = movements
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => x.Currency)
            .FirstOrDefault() ?? "INR";

        return Task.FromResult(new InventoryBalanceDto(
            stockLocationId,
            ingredientId,
            quantity,
            value,
            currency));
    }

    public Task<IReadOnlyList<InventoryMovementDto>> GetLedgerAsync(
        Guid organizationId,
        Guid stockLocationId,
        Guid ingredientId,
        CancellationToken cancellationToken)
    {
        var result = _movements
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.StockLocationId.Value == stockLocationId &&
                x.IngredientId == ingredientId)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new InventoryMovementDto(
                x.Id.Value,
                x.StockLocationId.Value,
                x.IngredientId,
                x.Type.ToString().ToUpperInvariant(),
                x.Quantity,
                x.SignedQuantity,
                x.UnitCost,
                x.Currency,
                x.OccurredAt))
            .ToArray();

        return Task.FromResult<IReadOnlyList<InventoryMovementDto>>(result);
    }

    private decimal GetRawBalance(
        Guid organizationId,
        Guid locationId,
        Guid ingredientId)
    {
        var movements = _movements.Where(x =>
            x.OrganizationId == organizationId &&
            x.StockLocationId.Value == locationId &&
            x.IngredientId == ingredientId);

        return InventoryCalculator.QuantityOnHand(movements);
    }
}
