namespace GastronomIQ.Domain.Recipes;

public enum RecipeVersionStatus
{
    Draft,
    Published,
    Archived
}

public sealed record RecipeId(Guid Value);
public sealed record RecipeVersionId(Guid Value);

public sealed record RecipeIngredientLine
{
    public Guid IngredientId { get; init; }
    public decimal Quantity { get; init; }
    public Guid UnitId { get; init; }
    public decimal? WastePercentage { get; init; }

    public RecipeIngredientLine(
        Guid IngredientId,
        decimal Quantity,
        Guid UnitId,
        decimal? WastePercentage = null)
    {
        if (IngredientId == Guid.Empty)
            throw new ArgumentException("Ingredient is required.");

        if (Quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(Quantity));

        if (UnitId == Guid.Empty)
            throw new ArgumentException("Unit is required.");

        if (WastePercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(WastePercentage));

        this.IngredientId = IngredientId;
        this.Quantity = Quantity;
        this.UnitId = UnitId;
        this.WastePercentage = WastePercentage;
    }
}

public sealed class Recipe
{
    private readonly List<RecipeVersion> _versions = new();

    public RecipeId Id { get; init; } = new(Guid.NewGuid());
    public Guid OrganizationId { get; init; }
    public string Name { get; private set; }
    public string Code { get; private set; }

    public IReadOnlyCollection<RecipeVersion> Versions => _versions.AsReadOnly();

    public Recipe(Guid organizationId, string name, string code)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Recipe name is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Recipe code is required.");

        OrganizationId = organizationId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
    }

    public RecipeVersion CreateDraft(
        decimal yieldQuantity,
        Guid yieldUnitId,
        string? method = null)
    {
        if (yieldQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(yieldQuantity));

        if (yieldUnitId == Guid.Empty)
            throw new ArgumentException("Yield unit is required.");

        var nextVersion = _versions.Count == 0
            ? 1
            : _versions.Max(v => v.VersionNumber) + 1;

        var draft = new RecipeVersion(
            Id,
            nextVersion,
            yieldQuantity,
            yieldUnitId,
            method);

        _versions.Add(draft);
        return draft;
    }

    public RecipeVersion? PublishedVersion =>
        _versions
            .Where(v => v.Status == RecipeVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
}

public sealed class RecipeVersion
{
    private readonly List<RecipeIngredientLine> _ingredients = new();

    public RecipeVersionId Id { get; init; } = new(Guid.NewGuid());
    public RecipeId RecipeId { get; init; }
    public int VersionNumber { get; init; }
    public RecipeVersionStatus Status { get; private set; } = RecipeVersionStatus.Draft;
    public decimal YieldQuantity { get; private set; }
    public Guid YieldUnitId { get; private set; }
    public string? Method { get; private set; }

    public IReadOnlyCollection<RecipeIngredientLine> Ingredients =>
        _ingredients.AsReadOnly();

    public RecipeVersion(
        RecipeId recipeId,
        int versionNumber,
        decimal yieldQuantity,
        Guid yieldUnitId,
        string? method)
    {
        RecipeId = recipeId;
        VersionNumber = versionNumber;
        YieldQuantity = yieldQuantity;
        YieldUnitId = yieldUnitId;
        Method = method;
    }

    public void AddIngredient(RecipeIngredientLine line)
    {
        EnsureDraft();
        if (_ingredients.Any(x => x.IngredientId == line.IngredientId))
            throw new InvalidOperationException(
                "Ingredient already exists in this recipe version.");

        _ingredients.Add(line);
    }

    public void RemoveIngredient(Guid ingredientId)
    {
        EnsureDraft();
        _ingredients.RemoveAll(x => x.IngredientId == ingredientId);
    }

    public void UpdateMethod(string? method)
    {
        EnsureDraft();
        Method = method;
    }

    public void Publish()
    {
        EnsureDraft();

        if (_ingredients.Count == 0)
            throw new InvalidOperationException(
                "A recipe version must contain at least one ingredient.");

        Status = RecipeVersionStatus.Published;
    }

    public void Archive()
    {
        if (Status != RecipeVersionStatus.Published)
            throw new InvalidOperationException(
                "Only published recipe versions can be archived.");

        Status = RecipeVersionStatus.Archived;
    }

    private void EnsureDraft()
    {
        if (Status != RecipeVersionStatus.Draft)
            throw new InvalidOperationException(
                "Published and archived recipe versions are immutable.");
    }
}
