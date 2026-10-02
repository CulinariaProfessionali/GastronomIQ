using System.Collections.Concurrent;
using GastronomIQ.Application.Inventory;
using GastronomIQ.Application.Procurement;
using GastronomIQ.Domain.Procurement;

namespace GastronomIQ.Infrastructure.Procurement;

public sealed class InMemoryProcurementService : IProcurementService
{
    private readonly ConcurrentDictionary<Guid, SupplierDto> _suppliers = new();
    private readonly ConcurrentDictionary<Guid, PurchaseOrder> _orders = new();
    private readonly IInventoryService _inventory;

    public InMemoryProcurementService(IInventoryService inventory) =>
        _inventory = inventory;

    public Task<SupplierDto> CreateSupplierAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = new SupplierDto(
            Guid.NewGuid(),
            request.OrganizationId,
            request.Name.Trim(),
            request.Code.Trim().ToUpperInvariant(),
            true);

        _suppliers[supplier.Id] = supplier;
        return Task.FromResult(supplier);
    }

    public Task<PurchaseOrderDto> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!_suppliers.ContainsKey(request.SupplierId))
            throw new KeyNotFoundException("Supplier not found.");

        var order = new PurchaseOrder(
            request.OrganizationId,
            request.BranchId,
            new SupplierId(request.SupplierId),
            request.Number);

        _orders[order.Id.Value] = order;
        return Task.FromResult(Map(order));
    }

    public Task AddLineAsync(
        AddPurchaseOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        var order = GetOrder(request.PurchaseOrderId);

        order.AddLine(new PurchaseOrderLine(
            request.IngredientId,
            request.OrderedQuantity,
            request.UnitPrice,
            request.UnitId));

        return Task.CompletedTask;
    }

    public Task SubmitAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        GetOrder(purchaseOrderId).Submit();
        return Task.CompletedTask;
    }

    public Task ApproveAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        GetOrder(purchaseOrderId).Approve();
        return Task.CompletedTask;
    }

    public async Task<PurchaseOrderLineDto> ReceiveAsync(
        ReceivePurchaseOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        var order = GetOrder(request.PurchaseOrderId);
        var line = order.Lines.First(x => x.Id.Value == request.LineId);

        order.ReceiveLine(request.LineId, request.Quantity);

        await _inventory.PostMovementAsync(
            new PostInventoryMovementRequest(
                order.OrganizationId,
                order.BranchId,
                request.StockLocationId,
                line.IngredientId,
                "Receipt",
                request.Quantity,
                request.ReceivedUnitPrice,
                "INR",
                order.Id.Value),
            cancellationToken);

        return Map(line);
    }

    public Task<PurchaseOrderDto?> GetAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        if (!_orders.TryGetValue(purchaseOrderId, out var order) ||
            order.OrganizationId != organizationId)
            return Task.FromResult<PurchaseOrderDto?>(null);

        return Task.FromResult<PurchaseOrderDto?>(Map(order));
    }

    private PurchaseOrder GetOrder(Guid id) =>
        _orders.TryGetValue(id, out var order)
            ? order
            : throw new KeyNotFoundException("Purchase order not found.");

    private static PurchaseOrderDto Map(PurchaseOrder order) =>
        new(
            order.Id.Value,
            order.OrganizationId,
            order.BranchId,
            order.SupplierId.Value,
            order.Number,
            order.Status.ToString().ToUpperInvariant(),
            order.Lines.Select(Map).ToArray());

    private static PurchaseOrderLineDto Map(PurchaseOrderLine line) =>
        new(
            line.Id.Value,
            line.IngredientId,
            line.OrderedQuantity,
            line.ReceivedQuantity,
            line.OutstandingQuantity,
            line.UnitPrice,
            line.UnitId);
}
