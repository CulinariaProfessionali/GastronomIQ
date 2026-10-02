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
        InventoryMovementId Id,
        Guid OrganizationId,
        Guid BranchId,
        StockLocationId StockLocationId,
        Guid IngredientId,
        InventoryMovementType Type,
        decimal Quantity,
        decimal UnitCost,
        string Currency,
        Guid? ReferenceId,
        DateTimeOffset OccurredAt)
    {
        if (OrganizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (BranchId == Guid.Empty) throw new ArgumentException("Branch is required.");
        if (StockLocationId.Value == Guid.Empty) throw new ArgumentException("Stock location is required.");
        if (IngredientId == Guid.Empty) throw new ArgumentException("Ingredient is required.");
        if (Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(Quantity));
        if (UnitCost < 0) throw new ArgumentOutOfRangeException(nameof(UnitCost));

        this.Id = Id;
        this.OrganizationId = OrganizationId;
        this.BranchId = BranchId;
        this.StockLocationId = StockLocationId;
        this.IngredientId = IngredientId;
        this.Type = Type;
        this.Quantity = Quantity;
        this.UnitCost = UnitCost;
        this.Currency = Currency;
        this.ReferenceId = ReferenceId;
        this.OccurredAt = OccurredAt;
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
