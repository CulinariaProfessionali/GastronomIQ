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
