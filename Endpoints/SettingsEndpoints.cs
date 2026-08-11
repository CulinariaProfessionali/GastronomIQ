using GastronomIQ.Api.Security;
using GastronomIQ.Application.Settings;

namespace GastronomIQ.Api.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/settings")
            .RequireAuthorization()
            .WithTags("Settings");

        group.MapGet("/{key}", async (
            string key,
            HttpContext http,
            IOrganizationSettingsService service,
            CancellationToken ct) =>
        {
            var organizationId = http.User.GetOrganizationId();
            if (organizationId == Guid.Empty)
                return Results.Forbid();

            var result = await service.GetAsync(organizationId, key, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPut("/{key}", async (
            string key,
            object value,
            HttpContext http,
            IOrganizationSettingsService service,
            CancellationToken ct) =>
        {
            var organizationId = http.User.GetOrganizationId();
            if (organizationId == Guid.Empty)
                return Results.Forbid();

            await service.SetAsync(organizationId, key, value, ct);
            return Results.NoContent();
        });
    }
}
