using GastronomIQ.Application.MenuEngineering;
using GastronomIQ.Domain.MenuEngineering;
using Npgsql;

namespace GastronomIQ.Infrastructure.MenuEngineering;

public sealed class PostgresMenuEngineeringService : IMenuEngineeringService
{
    private readonly Persistence.PostgresConnectionFactory _factory;

    public PostgresMenuEngineeringService(
        Persistence.PostgresConnectionFactory factory) =>
        _factory = factory;

    public async Task<MenuItemDto> CreateAsync(
        CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            INSERT INTO menu_items
            (
                id, organization_id, branch_id,
                recipe_id, recipe_version_id,
                name, code, is_active
            )
            VALUES
            (
                @id, @organization_id, @branch_id,
                @recipe_id, @recipe_version_id,
                @name, @code, true
            )
            RETURNING id, organization_id, branch_id,
                      recipe_id, recipe_version_id,
                      name, code;
            """, connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("organization_id", request.OrganizationId);
        command.Parameters.AddWithValue("branch_id", request.BranchId);
        command.Parameters.AddWithValue("recipe_id", request.RecipeId);
        command.Parameters.AddWithValue("recipe_version_id", request.RecipeVersionId);
        command.Parameters.AddWithValue("name", request.Name.Trim());
        command.Parameters.AddWithValue("code", request.Code.Trim().ToUpperInvariant());

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Menu item was not created.");

        return new MenuItemDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetGuid(4),
            reader.GetString(5),
            reader.GetString(6),
            null,
            null);
    }

    public async Task SetPriceAsync(
        SetMenuPriceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SellingPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.SellingPrice));

        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new ArgumentException(
                "Currency is required.",
                nameof(request.Currency));

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var exists = new NpgsqlCommand("""
            SELECT 1
            FROM menu_items
            WHERE id = @id
              AND is_active = true;
            """, connection);

        exists.Parameters.AddWithValue("id", request.MenuItemId);

        if (await exists.ExecuteScalarAsync(cancellationToken) is null)
            throw new KeyNotFoundException("Menu item not found.");

        await using var command = new NpgsqlCommand("""
            INSERT INTO menu_item_prices
            (
                id, menu_item_id,
                selling_price, currency, effective_from
            )
            VALUES
            (
                @id, @menu_item_id,
                @selling_price, @currency, now()
            );
            """, connection);

        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("menu_item_id", request.MenuItemId);
        command.Parameters.AddWithValue("selling_price", request.SellingPrice);
        command.Parameters.AddWithValue(
            "currency",
            request.Currency.Trim().ToUpperInvariant());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuEngineeringResultDto>> AnalyzeAsync(
        MenuEngineeringRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            return Array.Empty<MenuEngineeringResultDto>();

        // Branch authorization is enforced before the performance data is
        // accepted. Each supplied menu item must belong to this branch.
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        var suppliedIds = request.Items
            .Select(x => x.MenuItemId)
            .Distinct()
            .ToArray();

        await using var command = new NpgsqlCommand("""
            SELECT id
            FROM menu_items
            WHERE branch_id = @branch
              AND id = ANY(@ids)
              AND is_active = true;
            """, connection);

        command.Parameters.AddWithValue("branch", request.BranchId);
        command.Parameters.AddWithValue("ids", suppliedIds);

        var authorized = new HashSet<Guid>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            authorized.Add(reader.GetGuid(0));

        var unauthorized = suppliedIds
            .Where(id => !authorized.Contains(id))
            .ToArray();

        if (unauthorized.Length > 0)
            throw new UnauthorizedAccessException(
                "One or more menu items do not belong to the requested branch.");

        var inputs = request.Items.Select(x =>
            new MenuPerformanceInput(
                x.MenuItemId,
                x.Name,
                x.SellingPrice,
                x.RecipeCost,
                x.QuantitySold));

        var classified = MenuEngineeringCalculator.Classify(inputs);

        return classified.Select(x =>
            new MenuEngineeringResultDto(
                x.MenuItemId,
                x.Name,
                x.SellingPrice,
                x.RecipeCost,
                x.QuantitySold,
                x.ContributionMargin,
                x.FoodCostPercentage,
                x.Classification.ToString()))
            .ToArray();
    }
}
