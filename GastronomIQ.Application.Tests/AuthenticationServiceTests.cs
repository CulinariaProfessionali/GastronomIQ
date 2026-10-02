using GastronomIQ.Application.Identity;
using GastronomIQ.Infrastructure.Identity;

namespace GastronomIQ.Application.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task Login_returns_token_for_valid_request()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService());
        var login = await service.LoginAsync(new LoginRequest("chef@gastronomiq.com", "StrongPassword123!"), CancellationToken.None);

        Assert.NotNull(login);
    }

    [Fact]
    public async Task Login_rejects_empty_email()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService());
        var login = await service.LoginAsync(new LoginRequest("", "StrongPassword123!"), CancellationToken.None);

        Assert.Null(login);
    }

    [Fact]
    public async Task Login_rejects_empty_password()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService());
        var login = await service.LoginAsync(new LoginRequest("chef@gastronomiq.com", ""), CancellationToken.None);

        Assert.Null(login);
    }

    [Fact]
    public async Task Refresh_returns_token_for_valid_request()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService());
        var refreshed = await service.RefreshAsync(new RefreshTokenRequest("refresh-token"), CancellationToken.None);

        Assert.NotNull(refreshed);
    }

    [Fact]
    public async Task Refresh_rejects_empty_refresh_token()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService());
        var refreshed = await service.RefreshAsync(new RefreshTokenRequest(""), CancellationToken.None);

        Assert.Null(refreshed);
    }

    private sealed class FakeTokenService : ITokenService
    {
        public TokenResponse CreateToken(Guid userId, Guid organizationId, IEnumerable<string> permissions)
        {
            return new TokenResponse(
                AccessToken: $"{userId:N}-{organizationId:N}",
                RefreshToken: Guid.NewGuid().ToString("N"),
                ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(30));
        }
    }
}
