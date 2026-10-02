using GastronomIQ.Domain.Procurement;
using GastronomIQ.Domain.Production;

namespace GastronomIQ.Domain.Tests;

public class RollbackGuardTests
{
    [Fact]
    public void Procurement_receipt_failure_keeps_state_unchanged()
    {
        var order = new PurchaseOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SupplierId(Guid.NewGuid()),
            "PO-ROLLBACK");

        var line = new PurchaseOrderLine(
            Guid.NewGuid(),
            10,
            5,
            Guid.NewGuid());

        order.AddLine(line);
        order.Submit();
        order.Approve();

        var statusBefore = order.Status;
        var receivedBefore = line.ReceivedQuantity;

        Assert.Throws<InvalidOperationException>(() => order.ReceiveLine(line.Id.Value, 11));

        Assert.Equal(statusBefore, order.Status);
        Assert.Equal(receivedBefore, line.ReceivedQuantity);
    }

    [Fact]
    public void Production_complete_failure_keeps_state_unchanged()
    {
        var batch = new ProductionBatch(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PB-ROLLBACK",
            100,
            Guid.NewGuid());

        batch.AddLine(new ProductionBatchLine(
            Guid.NewGuid(),
            10,
            Guid.NewGuid(),
            3));

        batch.Start();
        var statusBefore = batch.Status;

        Assert.Throws<InvalidOperationException>(() => batch.Complete());

        Assert.Equal(statusBefore, batch.Status);
    }
}
