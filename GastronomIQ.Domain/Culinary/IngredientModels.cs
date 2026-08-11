namespace GastronomIQ.Domain.Culinary;

public sealed record IngredientId(Guid Value);
public sealed record IngredientCategoryId(Guid Value);
public sealed record UnitId(Guid Value);

public enum UnitType
{
    Weight,
    Volume,
    Count
}

public sealed class Unit
{
    public UnitId Id { get; init; } = new(Guid.NewGuid());
    public string Code { get; private set; }
    public string Name { get; private set; }
    public UnitType Type { get; private set; }
    public decimal ConversionToBase { get; private set; }

    public Unit(string code, string name, UnitType type, decimal conversionToBase)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Unit code is required.");
        if (conversionToBase <= 0) throw new ArgumentOutOfRangeException(nameof(conversionToBase));

        Code = code.Trim().ToLowerInvariant();
        Name = name.Trim();
        Type = type;
        ConversionToBase = conversionToBase;
    }

    public decimal ConvertToBase(decimal quantity) => quantity * ConversionToBase;
}

public sealed class Ingredient
{
    public IngredientId Id { get; init; } = new(Guid.NewGuid());
    public Guid OrganizationId { get; init; }
    public string Name { get; private set; }
    public string? Sku { get; private set; }
    public IngredientCategoryId? CategoryId { get; private set; }
    public UnitId BaseUnitId { get; private set; }
    public decimal CurrentUnitCost { get; private set; }
    public string Currency { get; private set; }

    public Ingredient(
        Guid organizationId,
        string name,
        UnitId baseUnitId,
        decimal currentUnitCost = 0,
        string currency = "INR",
        string? sku = null)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ingredient name is required.");
        if (currentUnitCost < 0) throw new ArgumentOutOfRangeException(nameof(currentUnitCost));

        OrganizationId = organizationId;
        Name = name.Trim();
        BaseUnitId = baseUnitId;
        CurrentUnitCost = currentUnitCost;
        Currency = currency.Trim().ToUpperInvariant();
        Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
    }

    public void ChangeCost(decimal cost)
    {
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        CurrentUnitCost = cost;
    }

    public void AssignCategory(IngredientCategoryId categoryId) => CategoryId = categoryId;
}
