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

public sealed record InventoryMovement
{
    public InventoryMovementId Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid BranchId { get; init; }
    public StockLocationId StockLocationId { get; init; }
    public Guid IngredientId { get; init; }
    public InventoryMovementType Type { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string Currency { get; init; }
    public Guid? ReferenceId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }

    public InventoryMovement(
        InventoryMovementId id,
        Guid organizationId,
        Guid branchId,
        StockLocationId stockLocationId,
        Guid ingredientId,
        InventoryMovementType type,
        decimal quantity,
        decimal unitCost,
        string currency,
        Guid? referenceId,
        DateTimeOffset occurredAt)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (branchId == Guid.Empty) throw new ArgumentException("Branch is required.");
        if (stockLocationId.Value == Guid.Empty) throw new ArgumentException("Stock location is required.");
        if (ingredientId == Guid.Empty) throw new ArgumentException("Ingredient is required.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.");

        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        StockLocationId = stockLocationId;
        IngredientId = ingredientId;
        Type = type;
        Quantity = quantity;
        UnitCost = unitCost;
        Currency = currency.Trim().ToUpperInvariant();
        ReferenceId = referenceId;
        OccurredAt = occurredAt;
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
