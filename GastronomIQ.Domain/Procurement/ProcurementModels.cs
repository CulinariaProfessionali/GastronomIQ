namespace GastronomIQ.Domain.Procurement;

public enum PurchaseOrderStatus
{
    Draft,
    Submitted,
    Approved,
    PartiallyReceived,
    Received,
    Closed,
    Cancelled
}

public sealed record SupplierId(Guid Value);
public sealed record PurchaseOrderId(Guid Value);
public sealed record PurchaseOrderLineId(Guid Value);

public sealed record SupplierIngredientPrice(
    Guid SupplierId,
    Guid IngredientId,
    decimal UnitPrice,
    string Currency,
    Guid UnitId,
    DateTimeOffset EffectiveFrom);

public sealed class PurchaseOrderLine
{
    public PurchaseOrderLineId Id { get; init; } = new(Guid.NewGuid());
    public Guid IngredientId { get; init; }
    public decimal OrderedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Guid UnitId { get; init; }

    public PurchaseOrderLine(
        Guid ingredientId,
        decimal orderedQuantity,
        decimal unitPrice,
        Guid unitId)
    {
        if (ingredientId == Guid.Empty) throw new ArgumentException("Ingredient is required.");
        if (orderedQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(orderedQuantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.");

        IngredientId = ingredientId;
        OrderedQuantity = orderedQuantity;
        UnitPrice = unitPrice;
        UnitId = unitId;
    }

    public decimal OutstandingQuantity => OrderedQuantity - ReceivedQuantity;

    public void Receive(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity > OutstandingQuantity)
            throw new InvalidOperationException("Receipt exceeds outstanding quantity.");

        ReceivedQuantity += quantity;
    }
}

public sealed class PurchaseOrder
{
    private readonly List<PurchaseOrderLine> _lines = new();

    public PurchaseOrderId Id { get; init; } = new(Guid.NewGuid());
    public Guid OrganizationId { get; init; }
    public Guid BranchId { get; init; }
    public SupplierId SupplierId { get; init; }
    public string Number { get; init; }
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public PurchaseOrder(
        Guid organizationId,
        Guid branchId,
        SupplierId supplierId,
        string number)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (branchId == Guid.Empty) throw new ArgumentException("Branch is required.");
        if (supplierId.Value == Guid.Empty) throw new ArgumentException("Supplier is required.");
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("PO number is required.");

        OrganizationId = organizationId;
        BranchId = branchId;
        SupplierId = supplierId;
        Number = number.Trim().ToUpperInvariant();
    }

    public void AddLine(PurchaseOrderLine line)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only draft purchase orders can be edited.");

        if (_lines.Any(x => x.IngredientId == line.IngredientId))
            throw new InvalidOperationException("Ingredient already exists on purchase order.");

        _lines.Add(line);
    }

    public void Submit()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("Purchase order requires at least one line.");

        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Purchase order is not in draft state.");

        Status = PurchaseOrderStatus.Submitted;
    }

    public void Approve()
    {
        if (Status != PurchaseOrderStatus.Submitted)
            throw new InvalidOperationException("Only submitted orders can be approved.");

        Status = PurchaseOrderStatus.Approved;
    }

    public void ReceiveLine(Guid lineId, decimal quantity)
    {
        if (Status is not (PurchaseOrderStatus.Approved
            or PurchaseOrderStatus.PartiallyReceived))
            throw new InvalidOperationException("Purchase order is not receivable.");

        var line = _lines.First(x => x.Id.Value == lineId);
        line.Receive(quantity);

        Status = _lines.All(x => x.OutstandingQuantity == 0)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;
    }

    public void Close()
    {
        if (Status != PurchaseOrderStatus.Received)
            throw new InvalidOperationException("Only fully received orders can be closed.");

        Status = PurchaseOrderStatus.Closed;
    }
}

public static class PurchasePriceVarianceCalculator
{
    public static decimal Calculate(
        decimal orderedUnitPrice,
        decimal receivedUnitPrice,
        decimal quantity)
    {
        if (orderedUnitPrice < 0 || receivedUnitPrice < 0 || quantity < 0)
            throw new ArgumentOutOfRangeException();

        return decimal.Round(
            (receivedUnitPrice - orderedUnitPrice) * quantity,
            4,
            MidpointRounding.AwayFromZero);
    }
}
