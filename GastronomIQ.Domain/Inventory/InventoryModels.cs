namespace GastronomIQ.Domain.Inventory;

public enum InventoryMovementType
{
    Opening,
    Receipt,
    TransferIn,
    TransferOut,
    Adjustment,
    Waste,
    Consumption
}

public sealed record StockLocationId(Guid Value);
public sealed record InventoryItemId(Guid Value);
public sealed record InventoryMovementId(Guid Value);

public sealed record InventoryMovement(
    InventoryMovementId Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid StockLocationId,
    Guid IngredientId,
    InventoryMovementType Type,
    decimal Quantity,
    decimal UnitCost,
    string Currency,
    Guid? ReferenceId,
    DateTimeOffset OccurredAt)
{
    public InventoryMovement
        : this(Id, OrganizationId, BranchId, StockLocationId, IngredientId,
            Type, Quantity, UnitCost, Currency, ReferenceId, OccurredAt)
    {
        if (OrganizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (BranchId == Guid.Empty) throw new ArgumentException("Branch is required.");
        if (StockLocationId == Guid.Empty) throw new ArgumentException("Stock location is required.");
        if (IngredientId == Guid.Empty) throw new ArgumentException("Ingredient is required.");
        if (Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(Quantity));
        if (UnitCost < 0) throw new ArgumentOutOfRangeException(nameof(UnitCost));
    }

    public decimal SignedQuantity =>
        Type is InventoryMovementType.TransferOut
            or InventoryMovementType.Waste
            or InventoryMovementType.Consumption
            ? -Quantity
            : Quantity;
}

public static class InventoryCalculator
{
    public static decimal QuantityOnHand(IEnumerable<InventoryMovement> movements) =>
        decimal.Round(movements.Sum(x => x.SignedQuantity), 6);

    public static decimal StockValue(IEnumerable<InventoryMovement> movements) =>
        decimal.Round(
            movements.Sum(x => x.SignedQuantity * x.UnitCost),
            4,
            MidpointRounding.AwayFromZero);
}
