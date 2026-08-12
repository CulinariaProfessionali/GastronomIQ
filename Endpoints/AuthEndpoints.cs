using GastronomIQ.Application.Identity;

namespace GastronomIQ.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterRequest request,
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
            var result = await auth.RegisterAsync(request, ct);

            return result.Status switch
            {
                RegistrationStatus.Success when result.Tokens is not null && result.UserId.HasValue =>
                    Results.Created($"/api/v1/users/{result.UserId}", result.Tokens),
                RegistrationStatus.InvalidRequest =>
                    Results.BadRequest(new { error = "Invalid registration payload." }),
                RegistrationStatus.EmailAlreadyExists =>
                    Results.Conflict(new { error = "Email already registered." }),
                _ => Results.Problem("Registration failed.")
            };
        });

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

        group.MapPost("/logout", async (
            LogoutRequest request,
            IAuthenticationService auth,
            CancellationToken ct) =>
        {
            var revoked = await auth.LogoutAsync(request, ct);
            return revoked ? Results.NoContent() : Results.Unauthorized();
        });
    }
}
