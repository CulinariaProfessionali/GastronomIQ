namespace GastronomIQ.Application.Culinary;

public sealed record CreateIngredientRequest(
    Guid OrganizationId,
    string Name,
    Guid BaseUnitId,
    decimal CurrentUnitCost,
    string Currency,
    string? Sku,
    Guid? CategoryId);

public sealed record UpdateIngredientRequest(
    string Name,
    decimal CurrentUnitCost,
    string Currency,
    string? Sku,
    Guid? CategoryId);

public sealed record IngredientDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    Guid BaseUnitId,
    decimal CurrentUnitCost,
    string Currency,
    string? Sku,
    Guid? CategoryId);

public sealed record CreateUnitRequest(
    Guid OrganizationId,
    string Code,
    string Name,
    string UnitType,
    decimal ConversionToBase);

public sealed record UnitDto(
    Guid Id,
    string Code,
    string Name,
    string UnitType,
    decimal ConversionToBase);

public sealed record CreateIngredientCategoryRequest(
    Guid OrganizationId,
    string Name);

public sealed record IngredientCategoryDto(
    Guid Id,
    string Name);

public interface IIngredientService
{
    Task<IngredientDto> CreateAsync(
        CreateIngredientRequest request,
        CancellationToken cancellationToken);

    Task<IngredientDto?> GetAsync(
        Guid organizationId,
        Guid ingredientId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IngredientDto>> SearchAsync(
        Guid organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IUnitConversionService
{
    decimal Convert(decimal quantity, Guid fromUnitId, Guid toUnitId);
}
