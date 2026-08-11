using GastronomIQ.Application.Procurement;

namespace GastronomIQ.Api.Endpoints;

public static class ProcurementEndpoints
{
    public static void MapProcurementEndpoints(this WebApplication app)
    {
        var suppliers = app.MapGroup("/api/v1/procurement/suppliers")
            .WithTags("Procurement");

        suppliers.MapPost("/", async (
            CreateSupplierRequest request,
            IProcurementService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateSupplierAsync(request, ct);
            return Results.Created(
                $"/api/v1/procurement/suppliers/{result.Id}",
                result);
        });

        var orders = app.MapGroup("/api/v1/procurement/orders")
            .WithTags("Procurement");

        orders.MapPost("/", async (
            CreatePurchaseOrderRequest request,
            IProcurementService service,
            CancellationToken ct) =>
        {
            var result = await service.CreatePurchaseOrderAsync(request, ct);
            return Results.Created(
                $"/api/v1/procurement/orders/{result.Id}",
                result);
        });

        orders.MapPost("/{id:guid}/lines", async (
            Guid id,
            AddPurchaseOrderLineRequest request,
            IProcurementService service,
            CancellationToken ct) =>
        {
            if (id != request.PurchaseOrderId)
                return Results.BadRequest(new { error = "Purchase order ID mismatch." });

            await service.AddLineAsync(request, ct);
            return Results.NoContent();
        });

        orders.MapPost("/{id:guid}/submit", async (
            Guid id,
            IProcurementService service,
            CancellationToken ct) =>
        {
            await service.SubmitAsync(id, ct);
            return Results.NoContent();
        });

        orders.MapPost("/{id:guid}/approve", async (
            Guid id,
            IProcurementService service,
            CancellationToken ct) =>
        {
            await service.ApproveAsync(id, ct);
            return Results.NoContent();
        });

        orders.MapPost("/{id:guid}/receive", async (
            Guid id,
            ReceivePurchaseOrderLineRequest request,
            IProcurementService service,
            CancellationToken ct) =>
        {
            if (id != request.PurchaseOrderId)
                return Results.BadRequest(new { error = "Purchase order ID mismatch." });

            var result = await service.ReceiveAsync(request, ct);
            return Results.Ok(result);
        });

        orders.MapGet("/{id:guid}", async (
            Guid id,
            Guid organizationId,
            IProcurementService service,
            CancellationToken ct) =>
        {
            var result = await service.GetAsync(
                organizationId,
                id,
                ct);

            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}
