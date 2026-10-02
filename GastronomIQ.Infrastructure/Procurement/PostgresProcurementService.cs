using GastronomIQ.Application.Procurement;
using GastronomIQ.Domain.Procurement;
using Npgsql;

namespace GastronomIQ.Infrastructure.Procurement;

public sealed class PostgresProcurementService : IProcurementService
{
    private readonly Persistence.PostgresConnectionFactory _factory;

    public PostgresProcurementService(
        Persistence.PostgresConnectionFactory factory) =>
        _factory = factory;

    public async Task<SupplierDto> CreateSupplierAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            INSERT INTO suppliers
                (id, organization_id, name, code)
            VALUES
                (@id, @organization_id, @name, @code)
            RETURNING id, organization_id, name, code, is_active;
            """, connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("organization_id", request.OrganizationId);
        command.Parameters.AddWithValue("name", request.Name.Trim());
        command.Parameters.AddWithValue("code", request.Code.Trim().ToUpperInvariant());

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Supplier was not created.");

        return new SupplierDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetBoolean(4));
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var supplier = new NpgsqlCommand("""
            SELECT 1
            FROM suppliers
            WHERE id = @supplier
              AND organization_id = @organization_id
              AND is_active = true;
            """, connection);

        supplier.Parameters.AddWithValue("supplier", request.SupplierId);
        supplier.Parameters.AddWithValue(
            "organization_id",
            request.OrganizationId);

        if (await supplier.ExecuteScalarAsync(cancellationToken) is null)
            throw new KeyNotFoundException("Supplier not found.");

        await using var command = new NpgsqlCommand("""
            INSERT INTO purchase_orders
                (id, organization_id, branch_id, supplier_id, number, status)
            VALUES
                (@id, @organization_id, @branch_id, @supplier_id, @number, 'DRAFT')
            RETURNING id, organization_id, branch_id, supplier_id, number, status;
            """, connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue(
            "organization_id",
            request.OrganizationId);
        command.Parameters.AddWithValue("branch_id", request.BranchId);
        command.Parameters.AddWithValue("supplier_id", request.SupplierId);
        command.Parameters.AddWithValue(
            "number",
            request.Number.Trim().ToUpperInvariant());

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Purchase order was not created.");

        return new PurchaseOrderDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetString(4),
            reader.GetString(5),
            Array.Empty<PurchaseOrderLineDto>());
    }

    public async Task AddLineAsync(
        AddPurchaseOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var status = new NpgsqlCommand("""
            SELECT status, organization_id
            FROM purchase_orders
            WHERE id = @id;
            """, connection);

        status.Parameters.AddWithValue("id", request.PurchaseOrderId);

        await using var reader =
            await status.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new KeyNotFoundException("Purchase order not found.");

        var currentStatus = reader.GetString(0);
        await reader.CloseAsync();

        if (!string.Equals(currentStatus, "DRAFT",
            StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Only draft purchase orders can be edited.");

        await using var duplicate = new NpgsqlCommand("""
            SELECT 1
            FROM purchase_order_lines
            WHERE purchase_order_id = @po
              AND ingredient_id = @ingredient;
            """, connection);

        duplicate.Parameters.AddWithValue("po", request.PurchaseOrderId);
        duplicate.Parameters.AddWithValue("ingredient", request.IngredientId);

        if (await duplicate.ExecuteScalarAsync(cancellationToken) is not null)
            throw new InvalidOperationException(
                "Ingredient already exists on purchase order.");

        await using var command = new NpgsqlCommand("""
            INSERT INTO purchase_order_lines
                (id, purchase_order_id, ingredient_id,
                 ordered_quantity, unit_price, unit_id)
            VALUES
                (@id, @po, @ingredient, @quantity, @price, @unit);
            """, connection);

        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("po", request.PurchaseOrderId);
        command.Parameters.AddWithValue("ingredient", request.IngredientId);
        command.Parameters.AddWithValue("quantity", request.OrderedQuantity);
        command.Parameters.AddWithValue("price", request.UnitPrice);
        command.Parameters.AddWithValue("unit", request.UnitId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task SubmitAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            purchaseOrderId,
            "DRAFT",
            "SUBMITTED",
            cancellationToken);

    public Task ApproveAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            purchaseOrderId,
            "SUBMITTED",
            "APPROVED",
            cancellationToken);

    public async Task<PurchaseOrderLineDto> ReceiveAsync(
        ReceivePurchaseOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var order = new NpgsqlCommand("""
                SELECT status, organization_id, branch_id
                FROM purchase_orders
                WHERE id = @po
                FOR UPDATE;
                """, connection, transaction);

            order.Parameters.AddWithValue("po", request.PurchaseOrderId);

            await using var orderReader =
                await order.ExecuteReaderAsync(cancellationToken);

            if (!await orderReader.ReadAsync(cancellationToken))
                throw new KeyNotFoundException("Purchase order not found.");

            var status = orderReader.GetString(0);
            var organizationId = orderReader.GetGuid(1);
            var branchId = orderReader.GetGuid(2);
            await orderReader.CloseAsync();

            if (status is not ("APPROVED" or "PARTIALLYRECEIVED"))
                throw new InvalidOperationException(
                    "Purchase order is not receivable.");

            await using var line = new NpgsqlCommand("""
                SELECT id, ingredient_id, ordered_quantity,
                       received_quantity, unit_price, unit_id
                FROM purchase_order_lines
                WHERE id = @line
                  AND purchase_order_id = @po
                FOR UPDATE;
                """, connection, transaction);

            line.Parameters.AddWithValue("line", request.LineId);
            line.Parameters.AddWithValue("po", request.PurchaseOrderId);

            await using var lineReader =
                await line.ExecuteReaderAsync(cancellationToken);

            if (!await lineReader.ReadAsync(cancellationToken))
                throw new KeyNotFoundException("Purchase order line not found.");

            var lineId = lineReader.GetGuid(0);
            var ingredientId = lineReader.GetGuid(1);
            var ordered = lineReader.GetDecimal(2);
            var received = lineReader.GetDecimal(3);
            var orderedPrice = lineReader.GetDecimal(4);
            var unitId = lineReader.GetGuid(5);
            await lineReader.CloseAsync();

            var outstanding = ordered - received;

            if (request.Quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.Quantity));

            if (request.Quantity > outstanding)
                throw new InvalidOperationException(
                    "Receipt exceeds outstanding quantity.");

            await using var location = new NpgsqlCommand("""
                SELECT 1
                FROM stock_locations
                WHERE id = @location
                  AND organization_id = @organization_id
                  AND branch_id = @branch_id;
                """, connection, transaction);

            location.Parameters.AddWithValue(
                "location",
                request.StockLocationId);
            location.Parameters.AddWithValue(
                "organization_id",
                organizationId);
            location.Parameters.AddWithValue(
                "branch_id",
                branchId);

            if (await location.ExecuteScalarAsync(cancellationToken) is null)
                throw new KeyNotFoundException(
                    "Stock location does not belong to the purchase-order branch.");

            var receiptId = Guid.NewGuid();

            await using var receipt = new NpgsqlCommand("""
                INSERT INTO purchase_receipts
                    (id, purchase_order_id, purchase_order_line_id,
                     stock_location_id, received_quantity,
                     received_unit_price)
                VALUES
                    (@id, @po, @line, @location, @quantity, @price);
                """, connection, transaction);

            receipt.Parameters.AddWithValue("id", receiptId);
            receipt.Parameters.AddWithValue("po", request.PurchaseOrderId);
            receipt.Parameters.AddWithValue("line", lineId);
            receipt.Parameters.AddWithValue(
                "location",
                request.StockLocationId);
            receipt.Parameters.AddWithValue(
                "quantity",
                request.Quantity);
            receipt.Parameters.AddWithValue(
                "price",
                request.ReceivedUnitPrice);

            await receipt.ExecuteNonQueryAsync(cancellationToken);

            await using var updateLine = new NpgsqlCommand("""
                UPDATE purchase_order_lines
                SET received_quantity = received_quantity + @quantity
                WHERE id = @line;
                """, connection, transaction);

            updateLine.Parameters.AddWithValue("quantity", request.Quantity);
            updateLine.Parameters.AddWithValue("line", lineId);

            await updateLine.ExecuteNonQueryAsync(cancellationToken);

            var newReceived = received + request.Quantity;
            var newStatus =
                await GetNewOrderStatusAsync(
                    connection,
                    transaction,
                    request.PurchaseOrderId);

            await using var updateOrder = new NpgsqlCommand("""
                UPDATE purchase_orders
                SET status = @status, updated_at = now()
                WHERE id = @po;
                """, connection, transaction);

            updateOrder.Parameters.AddWithValue("status", newStatus);
            updateOrder.Parameters.AddWithValue("po", request.PurchaseOrderId);

            await updateOrder.ExecuteNonQueryAsync(cancellationToken);

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
                    @location, @ingredient,
                    'RECEIPT', @quantity, @unit_cost,
                    'INR', @reference_id
                );
                """, connection, transaction);

            movement.Parameters.AddWithValue("id", Guid.NewGuid());
            movement.Parameters.AddWithValue(
                "organization_id",
                organizationId);
            movement.Parameters.AddWithValue("branch_id", branchId);
            movement.Parameters.AddWithValue(
                "location",
                request.StockLocationId);
            movement.Parameters.AddWithValue(
                "ingredient",
                ingredientId);
            movement.Parameters.AddWithValue(
                "quantity",
                request.Quantity);
            movement.Parameters.AddWithValue(
                "unit_cost",
                request.ReceivedUnitPrice);
            movement.Parameters.AddWithValue(
                "reference_id",
                receiptId);

            await movement.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new PurchaseOrderLineDto(
                lineId,
                ingredientId,
                ordered,
                newReceived,
                ordered - newReceived,
                orderedPrice,
                unitId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PurchaseOrderDto?> GetAsync(
        Guid organizationId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var order = new NpgsqlCommand("""
            SELECT id, organization_id, branch_id,
                   supplier_id, number, status
            FROM purchase_orders
            WHERE id = @id
              AND organization_id = @organization_id;
            """, connection);

        order.Parameters.AddWithValue("id", purchaseOrderId);
        order.Parameters.AddWithValue(
            "organization_id",
            organizationId);

        await using var reader =
            await order.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var dto = new PurchaseOrderDto(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetString(4),
            reader.GetString(5),
            Array.Empty<PurchaseOrderLineDto>());

        await reader.CloseAsync();

        await using var lines = new NpgsqlCommand("""
            SELECT id, ingredient_id, ordered_quantity,
                   received_quantity, unit_price, unit_id
            FROM purchase_order_lines
            WHERE purchase_order_id = @po
            ORDER BY id;
            """, connection);

        lines.Parameters.AddWithValue("po", purchaseOrderId);

        var lineDtos = new List<PurchaseOrderLineDto>();

        await using var lineReader =
            await lines.ExecuteReaderAsync(cancellationToken);

        while (await lineReader.ReadAsync(cancellationToken))
        {
            var ordered = lineReader.GetDecimal(2);
            var received = lineReader.GetDecimal(3);

            lineDtos.Add(new PurchaseOrderLineDto(
                lineReader.GetGuid(0),
                lineReader.GetGuid(1),
                ordered,
                received,
                ordered - received,
                lineReader.GetDecimal(4),
                lineReader.GetGuid(5)));
        }

        return dto with { Lines = lineDtos };
    }

    private async Task TransitionAsync(
        Guid purchaseOrderId,
        string from,
        string to,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            UPDATE purchase_orders
            SET status = @to, updated_at = now()
            WHERE id = @id
              AND status = @from;
            """, connection);

        command.Parameters.AddWithValue("id", purchaseOrderId);
        command.Parameters.AddWithValue("from", from);
        command.Parameters.AddWithValue("to", to);

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException(
                $"Purchase order cannot transition from {from} to {to}.");
    }

    private static async Task<string> GetNewOrderStatusAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid purchaseOrderId)
    {
        await using var command = new NpgsqlCommand("""
            SELECT
                COUNT(*) = COUNT(*) FILTER (
                    WHERE received_quantity >= ordered_quantity),
                BOOL_OR(received_quantity > 0)
            FROM purchase_order_lines
            WHERE purchase_order_id = @po;
            """, connection, transaction);

        command.Parameters.AddWithValue("po", purchaseOrderId);

        await using var reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        var allReceived = reader.GetBoolean(0);
        var anyReceived = !reader.IsDBNull(1) && reader.GetBoolean(1);

        return allReceived
            ? "RECEIVED"
            : anyReceived
                ? "PARTIALLYRECEIVED"
                : "APPROVED";
    }
}
