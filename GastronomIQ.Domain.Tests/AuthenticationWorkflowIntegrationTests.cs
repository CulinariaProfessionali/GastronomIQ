using GastronomIQ.Application.Identity;
using GastronomIQ.Infrastructure.Identity;

namespace GastronomIQ.Domain.Tests;

public class AuthenticationWorkflowIntegrationTests
{
    [Fact]
    public async Task Register_login_refresh_logout_flow_honors_token_lifecycle()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var register = await service.RegisterAsync(
            new RegisterRequest(
                "chef@gastronomiq.com",
                "StrongPassword123!",
                "Chef User",
                "Kitchen Team"),
            CancellationToken.None);

        Assert.Equal(RegistrationStatus.Success, register.Status);
        Assert.NotNull(register.Tokens);

        var login = await service.LoginAsync(
            new LoginRequest("chef@gastronomiq.com", "StrongPassword123!"),
            CancellationToken.None);

        Assert.NotNull(login);

        var refresh = await service.RefreshAsync(
            new RefreshTokenRequest(login!.RefreshToken),
            CancellationToken.None);

        Assert.NotNull(refresh);
        Assert.NotEqual(login.RefreshToken, refresh!.RefreshToken);

        var logout = await service.LogoutAsync(
            new LogoutRequest(refresh.RefreshToken),
            CancellationToken.None);

        Assert.True(logout);

        var replay = await service.RefreshAsync(
            new RefreshTokenRequest(refresh.RefreshToken),
            CancellationToken.None);

        Assert.Null(replay);
    }

    private sealed class FakeTokenService : ITokenService
    {
        public TokenResponse CreateToken(Guid userId, Guid organizationId, IEnumerable<string> permissions)
            => new(
                AccessToken: $"{userId:N}-{organizationId:N}",
                RefreshToken: Guid.NewGuid().ToString("N"),
                ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(30));
    }
}
