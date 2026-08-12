using GastronomIQ.Application.Identity;
using GastronomIQ.Infrastructure.Identity;

namespace GastronomIQ.Application.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task Register_allows_followup_login()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "chef@gastronomiq.com",
            "StrongPassword123!",
            "Chef User",
            "Kitchen Team");

        var registration = await service.RegisterAsync(request, CancellationToken.None);
        var login = await service.LoginAsync(
            new LoginRequest(request.Email, request.Password),
            CancellationToken.None);

        Assert.Equal(RegistrationStatus.Success, registration.Status);
        Assert.NotNull(registration.Tokens);
        Assert.NotNull(login);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "chef@gastronomiq.com",
            "StrongPassword123!",
            "Chef User",
            "Kitchen Team");

        await service.RegisterAsync(request, CancellationToken.None);
        var duplicate = await service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegistrationStatus.EmailAlreadyExists, duplicate.Status);
        Assert.Null(duplicate.Tokens);
    }

    [Fact]
    public async Task Login_rejects_wrong_password_for_registered_user()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "chef@gastronomiq.com",
            "StrongPassword123!",
            "Chef User",
            "Kitchen Team");

        await service.RegisterAsync(request, CancellationToken.None);
        var login = await service.LoginAsync(
            new LoginRequest(request.Email, "wrong-password"),
            CancellationToken.None);

        Assert.Null(login);
    }

    [Fact]
    public async Task Register_rejects_overly_long_password()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "chef@gastronomiq.com",
            new string('a', 1025),
            "Chef User",
            "Kitchen Team");

        var registration = await service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegistrationStatus.InvalidRequest, registration.Status);
        Assert.Null(registration.Tokens);
    }

    [Fact]
    public async Task Register_rejects_empty_email()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "",
            "StrongPassword123!",
            "Chef User",
            "Kitchen Team");

        var registration = await service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegistrationStatus.InvalidRequest, registration.Status);
        Assert.Null(registration.Tokens);
    }

    [Fact]
    public async Task Register_rejects_invalid_email_format()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "not-an-email",
            "StrongPassword123!",
            "Chef User",
            "Kitchen Team");

        var registration = await service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegistrationStatus.InvalidRequest, registration.Status);
        Assert.Null(registration.Tokens);
    }

    [Fact]
    public async Task Register_rejects_overly_long_organization_name()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "chef@gastronomiq.com",
            "StrongPassword123!",
            "Chef User",
            new string('o', 201));

        var registration = await service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(RegistrationStatus.InvalidRequest, registration.Status);
        Assert.Null(registration.Tokens);
    }

    [Fact]
    public async Task Refresh_rotates_refresh_token_and_rejects_old_one()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "chef@gastronomiq.com",
            "StrongPassword123!",
            "Chef User",
            "Kitchen Team");

        var registration = await service.RegisterAsync(request, CancellationToken.None);
        Assert.NotNull(registration.Tokens);

        var rotated = await service.RefreshAsync(
            new RefreshTokenRequest(registration.Tokens!.RefreshToken),
            CancellationToken.None);

        Assert.NotNull(rotated);
        Assert.NotEqual(registration.Tokens.RefreshToken, rotated!.RefreshToken);

        var replay = await service.RefreshAsync(
            new RefreshTokenRequest(registration.Tokens.RefreshToken),
            CancellationToken.None);

        Assert.Null(replay);
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var service = new InMemoryAuthenticationService(new FakeTokenService(), new InMemoryPasswordHasher());
        var request = new RegisterRequest(
            "chef@gastronomiq.com",
            "StrongPassword123!",
            "Chef User",
            "Kitchen Team");

        var registration = await service.RegisterAsync(request, CancellationToken.None);
        Assert.NotNull(registration.Tokens);

        var revoked = await service.LogoutAsync(
            new LogoutRequest(registration.Tokens!.RefreshToken),
            CancellationToken.None);

        Assert.True(revoked);

        var refreshed = await service.RefreshAsync(
            new RefreshTokenRequest(registration.Tokens.RefreshToken),
            CancellationToken.None);

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
