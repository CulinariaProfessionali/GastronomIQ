using GastronomIQ.Application.Identity;
using GastronomIQ.Infrastructure.Identity;

namespace GastronomIQ.Domain.Tests;

public class AuthenticationWorkflowIntegrationTests
{
    [Fact]
    public async Task Login_then_refresh_generates_tokens()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService());

        var login = await service.LoginAsync(
            new LoginRequest("chef@gastronomiq.com", "StrongPassword123!"),
            CancellationToken.None);

        Assert.NotNull(login);

        var refresh = await service.RefreshAsync(
            new RefreshTokenRequest(login!.RefreshToken),
            CancellationToken.None);

        Assert.NotNull(refresh);
        Assert.NotEqual(login.RefreshToken, refresh!.RefreshToken);
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
