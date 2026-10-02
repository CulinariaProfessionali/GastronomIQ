using GastronomIQ.Application.Production;
using Npgsql;

namespace GastronomIQ.Infrastructure.Production;

public sealed class PostgresProductionService : IProductionService
{
    private readonly Persistence.PostgresConnectionFactory _factory;

    public PostgresProductionService(
        Persistence.PostgresConnectionFactory factory) =>
        _factory = factory;

    public async Task<ProductionBatchDto> CreateAsync(
        CreateProductionBatchRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            INSERT INTO production_batches
            (
                id, organization_id, branch_id,
                recipe_id, recipe_version_id, number,
                status, planned_yield, yield_unit_id
            )
            VALUES
            (
                @id, @organization_id, @branch_id,
                @recipe_id, @recipe_version_id, @number,
                'PLANNED', @planned_yield, @yield_unit_id
            )
            RETURNING id, organization_id, branch_id,
                      recipe_id, recipe_version_id, number,
                      status, planned_yield,
                      COALESCE(actual_yield, 0),
                      waste_quantity;
            """, connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("organization_id", request.OrganizationId);
        command.Parameters.AddWithValue("branch_id", request.BranchId);
        command.Parameters.AddWithValue("recipe_id", request.RecipeId);
        command.Parameters.AddWithValue("recipe_version_id", request.RecipeVersionId);
        command.Parameters.AddWithValue("number", request.Number.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("planned_yield", request.PlannedYield);
        command.Parameters.AddWithValue("yield_unit_id", request.YieldUnitId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Production batch was not created.");

        return new ProductionBatchDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetGuid(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetDecimal(7),
            reader.GetDecimal(8),
            reader.GetDecimal(9),
            0,
            0,
            0,
            Array.Empty<ProductionBatchLineDto>());
    }

    public async Task AddLineAsync(
        AddProductionLineRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var batch = new NpgsqlCommand("""
            SELECT status
            FROM production_batches
            WHERE id = @id;
            """, connection);

        batch.Parameters.AddWithValue("id", request.ProductionBatchId);

        var status = await batch.ExecuteScalarAsync(cancellationToken);

        if (status is null)
            throw new KeyNotFoundException("Production batch not found.");

        if (!string.Equals(
                status.ToString(),
                "PLANNED",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Only planned production batches can be edited.");

        await using var duplicate = new NpgsqlCommand("""
            SELECT 1
            FROM production_batch_lines
            WHERE production_batch_id = @batch
              AND ingredient_id = @ingredient;
            """, connection);

        duplicate.Parameters.AddWithValue(
            "batch",
            request.ProductionBatchId);
        duplicate.Parameters.AddWithValue(
            "ingredient",
            request.IngredientId);

        if (await duplicate.ExecuteScalarAsync(cancellationToken) is not null)
            throw new InvalidOperationException(
                "Ingredient already exists on production batch.");

        await using var insert = new NpgsqlCommand("""
            INSERT INTO production_batch_lines
            (
                id, production_batch_id, ingredient_id,
                planned_quantity, actual_quantity,
                unit_id, unit_cost
            )
            VALUES
            (
                @id, @batch, @ingredient,
                @planned_quantity, 0,
                @unit_id, @unit_cost
            );
            """, connection);

        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        insert.Parameters.AddWithValue("batch", request.ProductionBatchId);
        insert.Parameters.AddWithValue("ingredient", request.IngredientId);
        insert.Parameters.AddWithValue("planned_quantity", request.PlannedQuantity);
        insert.Parameters.AddWithValue("unit_id", request.UnitId);
        insert.Parameters.AddWithValue("unit_cost", request.UnitCost);

        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task StartAsync(
        Guid productionBatchId,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            UPDATE production_batches
            SET status = 'INPROGRESS', updated_at = now()
            WHERE id = @id
              AND status = 'PLANNED'
              AND EXISTS (
                  SELECT 1
                  FROM production_batch_lines
                  WHERE production_batch_id = @id
              );
            """, connection);

        command.Parameters.AddWithValue("id", productionBatchId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException(
                "Production batch cannot be started.");
    }

    public async Task RecordLineAsync(
        RecordProductionLineRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ActualQuantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.ActualQuantity));

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            UPDATE production_batch_lines l
            SET actual_quantity = @quantity
            FROM production_batches b
            WHERE l.id = l.id
              AND l.production_batch_id = b.id
              AND l.production_batch_id = @batch
              AND l.ingredient_id = @ingredient
              AND b.status = 'INPROGRESS';
            """, connection);

        command.Parameters.AddWithValue(
            "quantity",
            request.ActualQuantity);
        command.Parameters.AddWithValue(
            "batch",
            request.ProductionBatchId);
        command.Parameters.AddWithValue(
            "ingredient",
            request.IngredientId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException(
                "Production line cannot be updated.");
    }

    public async Task<ProductionBatchDto> CompleteAsync(
        CompleteProductionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ActualYield <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.ActualYield));

        if (request.WasteQuantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.WasteQuantity));

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var batch = await LoadBatchForUpdateAsync(
                connection,
                transaction,
                request.ProductionBatchId,
                cancellationToken);

            if (batch is null)
                throw new KeyNotFoundException(
                    "Production batch not found.");

            if (!string.Equals(
                    batch.Status,
                    "INPROGRESS",
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Production batch must be in progress.");

            await ValidateStockLocationAsync(
                connection,
                transaction,
                batch.OrganizationId,
                batch.BranchId,
                request.StockLocationId,
                cancellationToken);

            var lines = await LoadLinesForUpdateAsync(
                connection,
                transaction,
                request.ProductionBatchId,
                cancellationToken);

            if (lines.Count == 0)
                throw new InvalidOperationException(
                    "Production batch has no ingredient lines.");

            foreach (var line in lines)
            {
                var actual =
                    line.ActualQuantity > 0
                        ? line.ActualQuantity
                        : line.PlannedQuantity;

                await EnsureSufficientStockAsync(
                    connection,
                    transaction,
                    batch.OrganizationId,
                    request.StockLocationId,
                    line.IngredientId,
                    actual,
                    cancellationToken);
            }

            var plannedCost = lines.Sum(
                x => x.PlannedQuantity * x.UnitCost);

            var actualCost = 0m;

            foreach (var line in lines)
            {
                var actual =
                    line.ActualQuantity > 0
                        ? line.ActualQuantity
                        : line.PlannedQuantity;

                actualCost += actual * line.UnitCost;

                await using var updateLine = new NpgsqlCommand("""
                    UPDATE production_batch_lines
                    SET actual_quantity = @actual
                    WHERE id = @id;
                    """, connection, transaction);

                updateLine.Parameters.AddWithValue("actual", actual);
                updateLine.Parameters.AddWithValue("id", line.Id);

                await updateLine.ExecuteNonQueryAsync(
                    cancellationToken);

                await using var movement = new NpgsqlCommand("""
                    INSERT INTO inventory_movements
                    (
                        id, organization_id, branch_id,
                        stock_location_id, ingredient_id,
                        movement_type, quantity, unit_cost,
                        currency, reference_id
                    )
                    VALUES
                    (
                        @id, @organization_id, @branch_id,
                        @stock_location_id, @ingredient_id,
                        'CONSUMPTION', @quantity, @unit_cost,
                        'INR', @reference_id
                    );
                    """, connection, transaction);

                movement.Parameters.AddWithValue(
                    "id",
                    Guid.NewGuid());
                movement.Parameters.AddWithValue(
                    "organization_id",
                    batch.OrganizationId);
                movement.Parameters.AddWithValue(
                    "branch_id",
                    batch.BranchId);
                movement.Parameters.AddWithValue(
                    "stock_location_id",
                    request.StockLocationId);
                movement.Parameters.AddWithValue(
                    "ingredient_id",
                    line.IngredientId);
                movement.Parameters.AddWithValue(
                    "quantity",
                    actual);
                movement.Parameters.AddWithValue(
                    "unit_cost",
                    line.UnitCost);
                movement.Parameters.AddWithValue(
                    "reference_id",
                    request.ProductionBatchId);

                await movement.ExecuteNonQueryAsync(
                    cancellationToken);
            }

            if (request.WasteQuantity > 0)
            {
                await using var waste = new NpgsqlCommand("""
                    INSERT INTO production_waste
                    (
                        id, production_batch_id,
                        stock_location_id, quantity,
                        unit_id, value
                    )
                    VALUES
                    (
                        @id, @batch, @location,
                        @quantity, @unit, @value
                    );
                    """, connection, transaction);

                waste.Parameters.AddWithValue(
                    "id",
                    Guid.NewGuid());
                waste.Parameters.AddWithValue(
                    "batch",
                    request.ProductionBatchId);
                waste.Parameters.AddWithValue(
                    "location",
                    request.StockLocationId);
                waste.Parameters.AddWithValue(
                    "quantity",
                    request.WasteQuantity);
                waste.Parameters.AddWithValue(
                    "unit",
                    batch.YieldUnitId);
                waste.Parameters.AddWithValue(
                    "value",
                    actualCost / request.ActualYield *
                    request.WasteQuantity);

                await waste.ExecuteNonQueryAsync(
                    cancellationToken);
            }

            await using var complete = new NpgsqlCommand("""
                UPDATE production_batches
                SET
                    status = 'COMPLETED',
                    actual_yield = @actual_yield,
                    waste_quantity = @waste_quantity,
                    updated_at = now()
                WHERE id = @id;
                """, connection, transaction);

            complete.Parameters.AddWithValue(
                "actual_yield",
                request.ActualYield);
            complete.Parameters.AddWithValue(
                "waste_quantity",
                request.WasteQuantity);
            complete.Parameters.AddWithValue(
                "id",
                request.ProductionBatchId);

            await complete.ExecuteNonQueryAsync(
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await GetAsync(
                       batch.OrganizationId,
                       request.ProductionBatchId,
                       cancellationToken)
                   ?? throw new InvalidOperationException(
                       "Completed production batch could not be reloaded.");
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);
            throw;
        }
    }

    public async Task<ProductionBatchDto?> GetAsync(
        Guid organizationId,
        Guid productionBatchId,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        var batch = await LoadBatchAsync(
            connection,
            null,
            organizationId,
            productionBatchId,
            cancellationToken);

        if (batch is null)
            return null;

        var lines = await LoadLinesAsync(
            connection,
            null,
            productionBatchId,
            cancellationToken);

        return Map(batch, lines);
    }

    private static async Task<BatchRow?> LoadBatchForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id, organization_id, branch_id,
                   recipe_id, recipe_version_id, number,
                   status, planned_yield,
                   COALESCE(actual_yield, 0),
                   waste_quantity, yield_unit_id
            FROM production_batches
            WHERE id = @id
            FOR UPDATE;
            """, connection, transaction);

        command.Parameters.AddWithValue("id", id);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await ReadBatchAsync(reader, cancellationToken);
    }

    private static async Task<BatchRow?> LoadBatchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id, organization_id, branch_id,
                   recipe_id, recipe_version_id, number,
                   status, planned_yield,
                   COALESCE(actual_yield, 0),
                   waste_quantity, yield_unit_id
            FROM production_batches
            WHERE id = @id
              AND organization_id = @organization_id;
            """, connection, transaction);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue(
            "organization_id",
            organizationId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await ReadBatchAsync(reader, cancellationToken);
    }

    private static async Task<BatchRow?> ReadBatchAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new BatchRow(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetGuid(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetDecimal(7),
            reader.GetDecimal(8),
            reader.GetDecimal(9),
            reader.GetGuid(10));
    }

    private static async Task<List<LineRow>> LoadLinesForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid batchId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id, ingredient_id,
                   planned_quantity, actual_quantity,
                   unit_id, unit_cost
            FROM production_batch_lines
            WHERE production_batch_id = @batch
            FOR UPDATE;
            """, connection, transaction);

        command.Parameters.AddWithValue("batch", batchId);

        return await ReadLinesAsync(
            command,
            cancellationToken);
    }

    private static async Task<List<LineRow>> LoadLinesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid batchId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id, ingredient_id,
                   planned_quantity, actual_quantity,
                   unit_id, unit_cost
            FROM production_batch_lines
            WHERE production_batch_id = @batch
            ORDER BY id;
            """, connection, transaction);

        command.Parameters.AddWithValue("batch", batchId);

        return await ReadLinesAsync(
            command,
            cancellationToken);
    }

    private static async Task<List<LineRow>> ReadLinesAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var result = new List<LineRow>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new LineRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetGuid(4),
                reader.GetDecimal(5)));
        }

        return result;
    }

    private static async Task ValidateStockLocationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid organizationId,
        Guid branchId,
        Guid stockLocationId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT 1
            FROM stock_locations
            WHERE id = @location
              AND organization_id = @organization_id
              AND branch_id = @branch_id;
            """, connection, transaction);

        command.Parameters.AddWithValue(
            "location",
            stockLocationId);
        command.Parameters.AddWithValue(
            "organization_id",
            organizationId);
        command.Parameters.AddWithValue(
            "branch_id",
            branchId);

        if (await command.ExecuteScalarAsync(cancellationToken) is null)
            throw new KeyNotFoundException(
                "Stock location does not belong to the production branch.");
    }

    private static async Task EnsureSufficientStockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid organizationId,
        Guid stockLocationId,
        Guid ingredientId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT COALESCE(SUM(
                CASE
                    WHEN UPPER(movement_type) IN
                        ('TRANSFEROUT','TRANSFER_OUT','WASTE','CONSUMPTION')
                    THEN -quantity
                    ELSE quantity
                END), 0)
            FROM inventory_movements
            WHERE organization_id = @organization_id
              AND stock_location_id = @location
              AND ingredient_id = @ingredient;
            """, connection, transaction);

        command.Parameters.AddWithValue(
            "organization_id",
            organizationId);
        command.Parameters.AddWithValue(
            "location",
            stockLocationId);
        command.Parameters.AddWithValue(
            "ingredient",
            ingredientId);

        var onHand = Convert.ToDecimal(
            await command.ExecuteScalarAsync(cancellationToken));

        if (onHand < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for ingredient {ingredientId}.");
    }

    private static ProductionBatchDto Map(
        BatchRow batch,
        IReadOnlyList<LineRow> lines)
    {
        var plannedCost = decimal.Round(
            lines.Sum(x => x.PlannedQuantity * x.UnitCost),
            4,
            MidpointRounding.AwayFromZero);

        var actualCost = decimal.Round(
            lines.Sum(x =>
                (x.ActualQuantity > 0
                    ? x.ActualQuantity
                    : x.PlannedQuantity) * x.UnitCost),
            4,
            MidpointRounding.AwayFromZero);

        return new ProductionBatchDto(
            batch.Id,
            batch.OrganizationId,
            batch.BranchId,
            batch.RecipeId,
            batch.RecipeVersionId,
            batch.Number,
            batch.Status,
            batch.PlannedYield,
            batch.ActualYield,
            batch.WasteQuantity,
            plannedCost,
            actualCost,
            decimal.Round(
                actualCost - plannedCost,
                4,
                MidpointRounding.AwayFromZero),
            lines.Select(x =>
                new ProductionBatchLineDto(
                    x.Id,
                    x.IngredientId,
                    x.PlannedQuantity,
                    x.ActualQuantity,
                    decimal.Round(
                        ((x.ActualQuantity > 0
                            ? x.ActualQuantity
                            : x.PlannedQuantity)
                        - x.PlannedQuantity) * x.UnitCost,
                        4,
                        MidpointRounding.AwayFromZero),
                    x.UnitId,
                    x.UnitCost)).ToArray());
    }

    private sealed record BatchRow(
        Guid Id,
        Guid OrganizationId,
        Guid BranchId,
        Guid RecipeId,
        Guid RecipeVersionId,
        string Number,
        string Status,
        decimal PlannedYield,
        decimal ActualYield,
        decimal WasteQuantity,
        Guid YieldUnitId);

    private sealed record LineRow(
        Guid Id,
        Guid IngredientId,
        decimal PlannedQuantity,
        decimal ActualQuantity,
        Guid UnitId,
        decimal UnitCost);
}
