using GastronomIQ.Application.Identity;

namespace GastronomIQ.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(request, ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(request, ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        });
    }
}
