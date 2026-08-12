using System.Collections.Concurrent;
using GastronomIQ.Application.Identity;

namespace GastronomIQ.Infrastructure.Identity;

public sealed class InMemoryAuthenticationService : IAuthenticationService
{
    private const int MaxEmailLength = 320;
    private const int MaxPasswordLength = 1024;
    private const int MaxDisplayNameLength = 200;
    private const int MaxOrganizationNameLength = 200;

    private readonly ITokenService _tokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ConcurrentDictionary<string, UserCredential> _users = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryAuthenticationService(ITokenService tokens, IPasswordHasher passwordHasher)
    {
        _tokens = tokens;
        _passwordHasher = passwordHasher;
    }

    public Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.DisplayName) ||
            string.IsNullOrWhiteSpace(request.OrganizationName) ||
            request.Email.Length > MaxEmailLength ||
            request.Password.Length > MaxPasswordLength ||
            request.DisplayName.Length > MaxDisplayNameLength ||
            request.OrganizationName.Length > MaxOrganizationNameLength)
        {
            return Task.FromResult(new RegistrationResult(
                RegistrationStatus.InvalidRequest,
                null));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (_users.ContainsKey(email))
        {
            return Task.FromResult(new RegistrationResult(
                RegistrationStatus.EmailAlreadyExists,
                null));
        }

        var user = new UserCredential(
            Guid.NewGuid(),
            email,
            _passwordHasher.Hash(request.Password),
            Guid.NewGuid(),
            new[]
            {
                "organization.read",
                "organization.manage",
                "branch.read",
                "ingredient.read",
                "ingredient.manage",
                "recipe.read",
                "recipe.manage"
            });

        if (!_users.TryAdd(email, user))
        {
            return Task.FromResult(new RegistrationResult(
                RegistrationStatus.EmailAlreadyExists,
                null));
        }

        return Task.FromResult(new RegistrationResult(
            RegistrationStatus.Success,
            _tokens.CreateToken(user.UserId, user.OrganizationId, user.Permissions),
            user.UserId));
    }

    public Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Email.Length > MaxEmailLength ||
            request.Password.Length > MaxPasswordLength)
            return Task.FromResult<TokenResponse?>(null);

        var email = request.Email.Trim().ToLowerInvariant();
        if (!_users.TryGetValue(email, out var user) || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Task.FromResult<TokenResponse?>(null);

        return Task.FromResult<TokenResponse?>(
            _tokens.CreateToken(user.UserId, user.OrganizationId, user.Permissions));
    }

    public Task<TokenResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Task.FromResult<TokenResponse?>(null);

        return Task.FromResult<TokenResponse?>(_tokens.CreateToken(
            Guid.NewGuid(), Guid.Empty, new[] { "organization.read" }));
    }

    private sealed record UserCredential(
        Guid UserId,
        string Email,
        string PasswordHash,
        Guid OrganizationId,
        IReadOnlyCollection<string> Permissions);
}
