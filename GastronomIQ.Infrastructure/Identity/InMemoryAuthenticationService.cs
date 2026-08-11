using GastronomIQ.Application.Identity;

namespace GastronomIQ.Infrastructure.Identity;

public sealed class InMemoryAuthenticationService : IAuthenticationService
{
    private readonly ITokenService _tokens;

    public InMemoryAuthenticationService(ITokenService tokens) => _tokens = tokens;

    public Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        // Development-only adapter. Production identity provider integration belongs here.
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Task.FromResult<TokenResponse?>(null);

        var userId = Guid.NewGuid();
        var organizationId = Guid.Empty;
        var permissions = new[] { "organization.read", "branch.read", "ingredient.read", "recipe.read" };

        return Task.FromResult<TokenResponse?>(_tokens.CreateToken(userId, organizationId, permissions));
    }

    public Task<TokenResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Task.FromResult<TokenResponse?>(null);

        return Task.FromResult<TokenResponse?>(_tokens.CreateToken(
            Guid.NewGuid(), Guid.Empty, new[] { "organization.read" }));
    }
}
