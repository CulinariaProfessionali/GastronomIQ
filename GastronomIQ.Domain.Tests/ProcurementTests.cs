using GastronomIQ.Domain.Procurement;

namespace GastronomIQ.Domain.Tests;

public class ProcurementTests
{
    [Fact]
    public void Purchase_order_requires_lines_before_submit()
    {
        var order = new PurchaseOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SupplierId(Guid.NewGuid()),
            "PO-001");

        Assert.Throws<InvalidOperationException>(() => order.Submit());
    }

    [Fact]
    public void Purchase_order_can_be_received_partially()
    {
        var order = new PurchaseOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SupplierId(Guid.NewGuid()),
            "PO-001");

        var line = new PurchaseOrderLine(
            Guid.NewGuid(),
            100,
            50,
            Guid.NewGuid());

        order.AddLine(line);
        order.Submit();
        order.Approve();
        order.ReceiveLine(line.Id.Value, 40);

        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, order.Status);
        Assert.Equal(60, line.OutstandingQuantity);
    }

    [Fact]
    public void Receipt_cannot_exceed_ordered_quantity()
    {
        var order = new PurchaseOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SupplierId(Guid.NewGuid()),
            "PO-001");

        var line = new PurchaseOrderLine(
            Guid.NewGuid(),
            100,
            50,
            Guid.NewGuid());

        order.AddLine(line);
        order.Submit();
        order.Approve();

        Assert.Throws<InvalidOperationException>(() =>
            order.ReceiveLine(line.Id.Value, 101));
    }

    [Fact]
    public void Purchase_price_variance_is_calculated()
    {
        var variance =
            PurchasePriceVarianceCalculator.Calculate(100, 110, 20);

        Assert.Equal(200, variance);
    }
}
