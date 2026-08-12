using GastronomIQ.Domain.Inventory;
using GastronomIQ.Domain.MenuEngineering;
using GastronomIQ.Domain.Procurement;
using GastronomIQ.Domain.Production;
using GastronomIQ.Domain.Reporting;

namespace GastronomIQ.Domain.Tests;

public class EndToEndWorkflowTests
{
    [Fact]
    public void Culinary_flow_smoke_completes_with_expected_kpis()
    {
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        var order = new PurchaseOrder(
            organizationId,
            branchId,
            new SupplierId(Guid.NewGuid()),
            "PO-E2E");

        var poLine = new PurchaseOrderLine(
            ingredientId,
            20,
            10,
            unitId);

        order.AddLine(poLine);
        order.Submit();
        order.Approve();
        order.ReceiveLine(poLine.Id.Value, 20);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);

        var opening = new InventoryMovement(
            new InventoryMovementId(Guid.NewGuid()),
            organizationId,
            branchId,
            new StockLocationId(Guid.NewGuid()),
            ingredientId,
            InventoryMovementType.Opening,
            50,
            10,
            "INR",
            null,
            DateTimeOffset.UtcNow);

        var receipt = new InventoryMovement(
            new InventoryMovementId(Guid.NewGuid()),
            organizationId,
            branchId,
            opening.StockLocationId,
            ingredientId,
            InventoryMovementType.Receipt,
            20,
            10,
            "INR",
            null,
            DateTimeOffset.UtcNow);

        var batch = new ProductionBatch(
            organizationId,
            branchId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PB-E2E",
            100,
            unitId);

        batch.AddLine(new ProductionBatchLine(ingredientId, 10, unitId, 10));
        batch.Start();
        batch.RecordActualIngredientQuantity(ingredientId, 12);
        batch.RecordActualYield(95);
        batch.RecordWaste(1);
        batch.Complete();
        Assert.Equal(ProductionBatchStatus.Completed, batch.Status);

        var consumption = new InventoryMovement(
            new InventoryMovementId(Guid.NewGuid()),
            organizationId,
            branchId,
            opening.StockLocationId,
            ingredientId,
            InventoryMovementType.Consumption,
            12,
            10,
            "INR",
            null,
            DateTimeOffset.UtcNow);

        var qoh = InventoryCalculator.QuantityOnHand(new[] { opening, receipt, consumption });
        Assert.Equal(58, qoh);

        var menu = MenuEngineeringCalculator.Classify(new[]
        {
            new MenuPerformanceInput(Guid.NewGuid(), "Signature Dish", 250, 120, 60)
        });

        var kpi = ReportingCalculator.FoodCostPercentage(
            menu.Single().RecipeCost,
            menu.Single().SellingPrice);

        Assert.Equal(48, kpi);
    }
}
