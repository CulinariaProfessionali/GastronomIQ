using GastronomIQ.Domain.Inventory;

namespace GastronomIQ.Domain.Tests;

public class InventoryTests
{
    private static InventoryMovement Movement(
        InventoryMovementType type,
        decimal quantity,
        decimal cost = 10m) =>
        new(
            new InventoryMovementId(Guid.NewGuid()),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Guid(),
            Guid.NewGuid(),
            type,
            quantity,
            cost,
            "INR",
            null,
            DateTimeOffset.UtcNow);

    [Fact]
    public void Receipt_increases_stock()
    {
        var movement = Movement(InventoryMovementType.Receipt, 10);

        Assert.Equal(10, movement.SignedQuantity);
    }

    [Fact]
    public void Waste_decreases_stock()
    {
        var movement = Movement(InventoryMovementType.Waste, 3);

        Assert.Equal(-3, movement.SignedQuantity);
    }

    [Fact]
    public void Quantity_on_hand_is_ledger_sum()
    {
        var movements = new[]
        {
            Movement(InventoryMovementType.Opening, 100),
            Movement(InventoryMovementType.Receipt, 25),
            Movement(InventoryMovementType.Waste, 5),
            Movement(InventoryMovementType.Consumption, 20)
        };

        Assert.Equal(100, InventoryCalculator.QuantityOnHand(movements));
    }

    [Fact]
    public void Stock_value_uses_signed_movement_value()
    {
        var movements = new[]
        {
            Movement(InventoryMovementType.Opening, 10, 100),
            Movement(InventoryMovementType.Waste, 2, 100)
        };

        Assert.Equal(800, InventoryCalculator.StockValue(movements));
    }
}
