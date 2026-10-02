using System.Collections.Concurrent;
using GastronomIQ.Application.Culinary;

namespace GastronomIQ.Infrastructure.Culinary;

public sealed class InMemoryIngredientService : IIngredientService
{
    private readonly ConcurrentDictionary<Guid, IngredientDto> _items = new();

    public Task<IngredientDto> CreateAsync(
        CreateIngredientRequest request,
        CancellationToken cancellationToken)
    {
        var item = new IngredientDto(
            Guid.NewGuid(),
            request.OrganizationId,
            request.Name.Trim(),
            request.BaseUnitId,
            request.CurrentUnitCost,
            request.Currency.ToUpperInvariant(),
            request.Sku,
            request.CategoryId);

        _items[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<IngredientDto?> GetAsync(
        Guid organizationId,
        Guid ingredientId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _items.TryGetValue(ingredientId, out var item) &&
            item.OrganizationId == organizationId
                ? item
                : null);
    }

    public Task<IReadOnlyList<IngredientDto>> SearchAsync(
        Guid organizationId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _items.Values
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Sku?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        var result = query
            .OrderBy(x => x.Name)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(Math.Min(pageSize, 100))
            .ToArray();

        return Task.FromResult<IReadOnlyList<IngredientDto>>(result);
    }
}
