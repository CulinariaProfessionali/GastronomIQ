namespace GastronomIQ.Application.Procurement;

public sealed record CreateSupplierRequest(
    Guid OrganizationId,
    string Name,
    string Code);

public sealed record SupplierDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Code,
    bool IsActive);

public sealed record CreatePurchaseOrderRequest(
    Guid OrganizationId,
    Guid BranchId,
    Guid SupplierId,
    string Number);

public sealed record AddPurchaseOrderLineRequest(
    Guid PurchaseOrderId,
    Guid IngredientId,
    decimal OrderedQuantity,
    decimal UnitPrice,
    Guid UnitId);

public sealed record PurchaseOrderLineDto(
    Guid Id,
    Guid IngredientId,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal OutstandingQuantity,
    decimal UnitPrice,
    Guid UnitId);

public sealed record PurchaseOrderDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid SupplierId,
    string Number,
    string Status,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record ReceivePurchaseOrderLineRequest(
    Guid PurchaseOrderId,
    Guid LineId,
    decimal Quantity,
    decimal ReceivedUnitPrice,
    Guid StockLocationId);

public sealed record PurchasePriceVarianceDto(
    Guid IngredientId,
    decimal OrderedUnitPrice,
    decimal ReceivedUnitPrice,
    decimal Quantity,
    decimal Variance);

public interface IProcurementService
{
    Task<SupplierDto> CreateSupplierAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken);

    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken);

    Task AddLineAsync(
        AddPurchaseOrderLineRequest request,
        CancellationToken cancellationToken);

    Task SubmitAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken);

    Task ApproveAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken);

    Task<PurchaseOrderLineDto> ReceiveAsync(
        ReceivePurchaseOrderLineRequest request,
        CancellationToken cancellationToken);

    Task<PurchaseOrderDto?> GetAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken);
}
