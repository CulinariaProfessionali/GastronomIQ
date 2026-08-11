using GastronomIQ.Domain.Production;

namespace GastronomIQ.Domain.Tests;

public class ProductionTests
{
    private static ProductionBatch Batch()
    {
        var batch = new ProductionBatch(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PB-001",
            100,
            Guid.NewGuid());

        batch.AddLine(new ProductionBatchLine(
            Guid.NewGuid(),
            10,
            Guid.NewGuid(),
            100));

        return batch;
    }

    [Fact]
    public void Batch_requires_lines_before_start()
    {
        var batch = new ProductionBatch(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PB-001",
            100,
            Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => batch.Start());
    }

    [Fact]
    public void Batch_can_complete_with_actual_yield()
    {
        var batch = Batch();
        batch.Start();
        batch.RecordActualYield(95);
        batch.RecordActualIngredientQuantity(
            batch.Lines.First().IngredientId,
            11);

        batch.Complete();

        Assert.Equal(ProductionBatchStatus.Completed, batch.Status);
        Assert.Equal(95, batch.ActualYield);
        Assert.Equal(1100, batch.ActualCost);
    }

    [Fact]
    public void Cost_variance_is_calculated()
    {
        var batch = Batch();
        batch.Start();
        batch.RecordActualYield(100);
        batch.RecordActualIngredientQuantity(
            batch.Lines.First().IngredientId,
            12);

        Assert.Equal(200, batch.CostVariance);
    }
}
