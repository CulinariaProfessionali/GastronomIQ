using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace GastronomIQ.Application.Tests;

public class AuthEndpointsIntegrationTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsIntegrationTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_login_refresh_logout_flow_returns_expected_status_codes()
    {
        var registerPayload = new
        {
            email = $"chef-{Guid.NewGuid():N}@gastronomiq.com",
            password = "StrongPassword123!",
            displayName = "Chef User",
            organizationName = "Kitchen Team"
        };

        var register = await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registeredTokens = await register.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(registeredTokens);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = registerPayload.email,
            password = registerPayload.password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginTokens = await login.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(loginTokens);

        var refresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = loginTokens!.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotatedTokens = await refresh.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(rotatedTokens);

        var logout = await _client.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken = rotatedTokens!.RefreshToken
        });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var replayRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = rotatedTokens.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replayRefresh.StatusCode);
    }

    [Fact]
    public async Task Register_returns_bad_request_for_invalid_email()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "not-an-email",
            password = "StrongPassword123!",
            displayName = "Chef User",
            organizationName = "Kitchen Team"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record TokenResponseDto(
        string AccessToken,
        string RefreshToken,
        DateTimeOffset ExpiresAt);
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "integration-tests-signing-key-at-least-32-bytes",
                ["Jwt:AccessTokenMinutes"] = "30"
            });
        });
    }
}
