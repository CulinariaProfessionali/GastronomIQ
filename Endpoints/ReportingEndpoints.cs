using GastronomIQ.Application.Reporting;

namespace GastronomIQ.Api.Endpoints;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/reporting")
            .WithTags("Reporting");

        group.MapPost("/operational-dashboard", async (
            OperationalDashboardRequest request,
            IReportingService service,
            CancellationToken ct) =>
        {
            var result = await service.BuildDashboardAsync(request, ct);
            return Results.Ok(result);
        });
    }
}
