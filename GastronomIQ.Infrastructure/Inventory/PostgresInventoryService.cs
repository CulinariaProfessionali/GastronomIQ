using GastronomIQ.Application.Inventory;
using Npgsql;

namespace GastronomIQ.Infrastructure.Inventory;

public sealed class PostgresInventoryService : IInventoryService
{
    private readonly Persistence.PostgresConnectionFactory _factory;

    public PostgresInventoryService(Persistence.PostgresConnectionFactory factory) =>
        _factory = factory;

    public async Task<StockLocationDto> CreateLocationAsync(
        CreateStockLocationRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(@"
            INSERT INTO stock_locations
                (id, organization_id, branch_id, name, code)
            VALUES
                (@id, @org, @branch, @name, @code)
            RETURNING id, organization_id, branch_id, name, code;", connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("org", request.OrganizationId);
        command.Parameters.AddWithValue("branch", request.BranchId);
        command.Parameters.AddWithValue("name", request.Name.Trim());
        command.Parameters.AddWithValue("code", request.Code.Trim().ToUpperInvariant());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Stock location was not created.");

        return new StockLocationDto(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2),
            reader.GetString(3), reader.GetString(4));
    }

    public async Task<InventoryMovementDto> PostMovementAsync(
        PostInventoryMovementRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var type = request.MovementType.Trim().ToUpperInvariant();
        var negative = type is "TRANSFEROUT" or "TRANSFER_OUT" or "WASTE" or "CONSUMPTION";

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (negative)
            {
                await using var balance = new NpgsqlCommand(@"
                    SELECT COALESCE(SUM(
                        CASE
                            WHEN UPPER(movement_type) IN
                                ('TRANSFEROUT','TRANSFER_OUT','WASTE','CONSUMPTION')
                            THEN -quantity
                            ELSE quantity
                        END), 0)
                    FROM inventory_movements
                    WHERE organization_id = @org
                      AND stock_location_id = @location
                      AND ingredient_id = @ingredient
                    FOR UPDATE;", connection, transaction);

                balance.Parameters.AddWithValue("org", request.OrganizationId);
                balance.Parameters.AddWithValue("location", request.StockLocationId);
                balance.Parameters.AddWithValue("ingredient", request.IngredientId);

                var onHand = Convert.ToDecimal(
                    await balance.ExecuteScalarAsync(cancellationToken));

                if (onHand < request.Quantity)
                    throw new InvalidOperationException("Insufficient stock.");
            }

            await using var insert = new NpgsqlCommand(@"
                INSERT INTO inventory_movements
                (id, organization_id, branch_id, stock_location_id,
                 ingredient_id, movement_type, quantity, unit_cost,
                 currency, reference_id)
                VALUES
                (@id, @org, @branch, @location, @ingredient, @type,
                 @quantity, @cost, @currency, @reference)
                RETURNING occurred_at;", connection, transaction);

            insert.Parameters.AddWithValue("id", id);
            insert.Parameters.AddWithValue("org", request.OrganizationId);
            insert.Parameters.AddWithValue("branch", request.BranchId);
            insert.Parameters.AddWithValue("location", request.StockLocationId);
            insert.Parameters.AddWithValue("ingredient", request.IngredientId);
            insert.Parameters.AddWithValue("type", type);
            insert.Parameters.AddWithValue("quantity", request.Quantity);
            insert.Parameters.AddWithValue("cost", request.UnitCost);
            insert.Parameters.AddWithValue("currency", request.Currency.ToUpperInvariant());
            insert.Parameters.AddWithValue("reference", (object?)request.ReferenceId ?? DBNull.Value);

            var occurredAt = (DateTimeOffset)(
                await insert.ExecuteScalarAsync(cancellationToken)
                ?? DateTimeOffset.UtcNow);

            await transaction.CommitAsync(cancellationToken);

            return new InventoryMovementDto(
                id, request.StockLocationId, request.IngredientId, type,
                request.Quantity, negative ? -request.Quantity : request.Quantity,
                request.UnitCost, request.Currency.ToUpperInvariant(), occurredAt);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InventoryBalanceDto> GetBalanceAsync(
        Guid organizationId,
        Guid stockLocationId,
        Guid ingredientId,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(@"
            SELECT
                COALESCE(SUM(
                    CASE
                        WHEN UPPER(movement_type) IN
                            ('TRANSFEROUT','TRANSFER_OUT','WASTE','CONSUMPTION')
                        THEN -quantity
                        ELSE quantity
                    END), 0),
                COALESCE(SUM(
                    CASE
                        WHEN UPPER(movement_type) IN
                            ('TRANSFEROUT','TRANSFER_OUT','WASTE','CONSUMPTION')
                        THEN -quantity * unit_cost
                        ELSE quantity * unit_cost
                    END), 0),
                COALESCE((ARRAY_AGG(currency ORDER BY occurred_at DESC))[1], 'INR')
            FROM inventory_movements
            WHERE organization_id = @org
              AND stock_location_id = @location
              AND ingredient_id = @ingredient;", connection);

        command.Parameters.AddWithValue("org", organizationId);
        command.Parameters.AddWithValue("location", stockLocationId);
        command.Parameters.AddWithValue("ingredient", ingredientId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new InventoryBalanceDto(stockLocationId, ingredientId, 0, 0, "INR");

        return new InventoryBalanceDto(
            stockLocationId, ingredientId, reader.GetDecimal(0),
            reader.GetDecimal(1), reader.GetString(2));
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetLedgerAsync(
        Guid organizationId,
        Guid stockLocationId,
        Guid ingredientId,
        CancellationToken cancellationToken)
    {
        var result = new List<InventoryMovementDto>();

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(@"
            SELECT id, movement_type, quantity, unit_cost, currency, occurred_at
            FROM inventory_movements
            WHERE organization_id = @org
              AND stock_location_id = @location
              AND ingredient_id = @ingredient
            ORDER BY occurred_at DESC;", connection);

        command.Parameters.AddWithValue("org", organizationId);
        command.Parameters.AddWithValue("location", stockLocationId);
        command.Parameters.AddWithValue("ingredient", ingredientId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var type = reader.GetString(1);
            var negative = type is "TRANSFEROUT" or "TRANSFER_OUT" or "WASTE" or "CONSUMPTION";
            var quantity = reader.GetDecimal(2);

            result.Add(new InventoryMovementDto(
                reader.GetGuid(0), stockLocationId, ingredientId, type,
                quantity, negative ? -quantity : quantity,
                reader.GetDecimal(3), reader.GetString(4),
                reader.GetFieldValue<DateTimeOffset>(5)));
        }

        return result;
    }
}
