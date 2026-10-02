namespace GastronomIQ.Domain.Production;

public enum ProductionBatchStatus
{
    Planned,
    InProgress,
    Completed,
    Closed,
    Cancelled
}

public sealed record ProductionBatchId(Guid Value);
public sealed record ProductionBatchLineId(Guid Value);

public sealed class ProductionBatchLine
{
    public ProductionBatchLineId Id { get; init; } = new(Guid.NewGuid());
    public Guid IngredientId { get; init; }
    public decimal PlannedQuantity { get; private set; }
    public decimal ActualQuantity { get; private set; }
    public Guid UnitId { get; init; }
    public decimal UnitCost { get; private set; }

    public ProductionBatchLine(
        Guid ingredientId,
        decimal plannedQuantity,
        Guid unitId,
        decimal unitCost)
    {
        if (ingredientId == Guid.Empty) throw new ArgumentException("Ingredient is required.");
        if (plannedQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(plannedQuantity));
        if (unitId == Guid.Empty) throw new ArgumentException("Unit is required.");
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));

        IngredientId = ingredientId;
        PlannedQuantity = plannedQuantity;
        UnitId = unitId;
        UnitCost = unitCost;
    }

    public void RecordActual(decimal quantity)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        ActualQuantity = quantity;
    }

    public decimal CostVariance =>
        decimal.Round(
            (ActualQuantity - PlannedQuantity) * UnitCost,
            4,
            MidpointRounding.AwayFromZero);
}

public sealed class ProductionBatch
{
    private readonly List<ProductionBatchLine> _lines = new();

    public ProductionBatchId Id { get; init; } = new(Guid.NewGuid());
    public Guid OrganizationId { get; init; }
    public Guid BranchId { get; init; }
    public Guid RecipeId { get; init; }
    public Guid RecipeVersionId { get; init; }
    public string Number { get; init; }
    public decimal PlannedYield { get; private set; }
    public decimal ActualYield { get; private set; }
    public Guid YieldUnitId { get; init; }
    public decimal WasteQuantity { get; private set; }
    public ProductionBatchStatus Status { get; private set; } = ProductionBatchStatus.Planned;

    public IReadOnlyCollection<ProductionBatchLine> Lines => _lines.AsReadOnly();

    public ProductionBatch(
        Guid organizationId,
        Guid branchId,
        Guid recipeId,
        Guid recipeVersionId,
        string number,
        decimal plannedYield,
        Guid yieldUnitId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (branchId == Guid.Empty) throw new ArgumentException("Branch is required.");
        if (recipeId == Guid.Empty) throw new ArgumentException("Recipe is required.");
        if (recipeVersionId == Guid.Empty) throw new ArgumentException("Recipe version is required.");
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Batch number is required.");
        if (plannedYield <= 0) throw new ArgumentOutOfRangeException(nameof(plannedYield));
        if (yieldUnitId == Guid.Empty) throw new ArgumentException("Yield unit is required.");

        OrganizationId = organizationId;
        BranchId = branchId;
        RecipeId = recipeId;
        RecipeVersionId = recipeVersionId;
        Number = number.Trim().ToUpperInvariant();
        PlannedYield = plannedYield;
        YieldUnitId = yieldUnitId;
    }

    public void AddLine(ProductionBatchLine line)
    {
        if (Status != ProductionBatchStatus.Planned)
            throw new InvalidOperationException("Only planned batches can be edited.");

        if (_lines.Any(x => x.IngredientId == line.IngredientId))
            throw new InvalidOperationException("Ingredient already exists on batch.");

        _lines.Add(line);
    }

    public void Start()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("Production batch requires ingredient lines.");

        if (Status != ProductionBatchStatus.Planned)
            throw new InvalidOperationException("Batch is not planned.");

        Status = ProductionBatchStatus.InProgress;
    }

    public void RecordActualYield(decimal actualYield)
    {
        if (Status != ProductionBatchStatus.InProgress)
            throw new InvalidOperationException("Batch must be in progress.");

        if (actualYield <= 0)
            throw new ArgumentOutOfRangeException(nameof(actualYield));

        ActualYield = actualYield;
    }

    public void RecordActualIngredientQuantity(Guid ingredientId, decimal quantity)
    {
        if (Status != ProductionBatchStatus.InProgress)
            throw new InvalidOperationException("Batch must be in progress.");

        var line = _lines.First(x => x.IngredientId == ingredientId);
        line.RecordActual(quantity);
    }

    public void RecordWaste(decimal quantity)
    {
        if (Status != ProductionBatchStatus.InProgress)
            throw new InvalidOperationException("Batch must be in progress.");

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        WasteQuantity += quantity;
    }

    public void Complete()
    {
        if (Status != ProductionBatchStatus.InProgress)
            throw new InvalidOperationException("Batch must be in progress.");

        if (ActualYield <= 0)
            throw new InvalidOperationException("Actual yield is required.");

        Status = ProductionBatchStatus.Completed;
    }

    public decimal PlannedCost =>
        decimal.Round(
            _lines.Sum(x => x.PlannedQuantity * x.UnitCost),
            4,
            MidpointRounding.AwayFromZero);

    public decimal ActualCost =>
        decimal.Round(
            _lines.Sum(x => x.ActualQuantity * x.UnitCost),
            4,
            MidpointRounding.AwayFromZero);

    public decimal CostVariance =>
        decimal.Round(ActualCost - PlannedCost, 4, MidpointRounding.AwayFromZero);
}
