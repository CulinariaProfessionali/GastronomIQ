using System.Collections.Concurrent;
using System.Net.Mail;
using GastronomIQ.Application.Identity;

namespace GastronomIQ.Infrastructure.Identity;

public sealed class InMemoryAuthenticationService : IAuthenticationService
{
    private const int MaxEmailLength = 320;
    private const int MaxPasswordLength = 1024;
    private const int MaxDisplayNameLength = 200;
    private const int MaxOrganizationNameLength = 200;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly ITokenService _tokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ConcurrentDictionary<string, UserCredential> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RefreshTokenState> _refreshTokens = new(StringComparer.Ordinal);

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
            request.Password.Length < 12 ||
            request.Email.Length > MaxEmailLength ||
            request.Password.Length > MaxPasswordLength ||
            request.DisplayName.Length > MaxDisplayNameLength ||
            request.OrganizationName.Length > MaxOrganizationNameLength ||
            !IsValidEmail(request.Email))
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

        var tokens = _tokens.CreateToken(user.UserId, user.OrganizationId, user.Permissions);
        StoreRefreshToken(tokens.RefreshToken, user.UserId, user.OrganizationId, user.Permissions);

        return Task.FromResult(new RegistrationResult(RegistrationStatus.Success, tokens, user.UserId));
    }

    public Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Email.Length > MaxEmailLength ||
            request.Password.Length > MaxPasswordLength ||
            !IsValidEmail(request.Email))
            return Task.FromResult<TokenResponse?>(null);

        var email = request.Email.Trim().ToLowerInvariant();
        if (!_users.TryGetValue(email, out var user) || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Task.FromResult<TokenResponse?>(null);

        var tokens = _tokens.CreateToken(user.UserId, user.OrganizationId, user.Permissions);
        StoreRefreshToken(tokens.RefreshToken, user.UserId, user.OrganizationId, user.Permissions);

        return Task.FromResult<TokenResponse?>(tokens);
    }

    public Task<TokenResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Task.FromResult<TokenResponse?>(null);

        var oldToken = request.RefreshToken.Trim();
        if (!_refreshTokens.TryGetValue(oldToken, out var state) || state.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _refreshTokens.TryRemove(oldToken, out _);
            return Task.FromResult<TokenResponse?>(null);
        }

        _refreshTokens.TryRemove(oldToken, out _);
        var tokens = _tokens.CreateToken(state.UserId, state.OrganizationId, state.Permissions);
        StoreRefreshToken(tokens.RefreshToken, state.UserId, state.OrganizationId, state.Permissions);

        return Task.FromResult<TokenResponse?>(tokens);
    }

    public Task<bool> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Task.FromResult(false);

        return Task.FromResult(_refreshTokens.TryRemove(request.RefreshToken.Trim(), out _));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void StoreRefreshToken(
        string refreshToken,
        Guid userId,
        Guid organizationId,
        IReadOnlyCollection<string> permissions)
    {
        _refreshTokens[refreshToken] = new RefreshTokenState(
            userId,
            organizationId,
            permissions,
            DateTimeOffset.UtcNow.Add(RefreshTokenLifetime));
    }

    private sealed record UserCredential(
        Guid UserId,
        string Email,
        string PasswordHash,
        Guid OrganizationId,
        IReadOnlyCollection<string> Permissions);

    private sealed record RefreshTokenState(
        Guid UserId,
        Guid OrganizationId,
        IReadOnlyCollection<string> Permissions,
        DateTimeOffset ExpiresAt);
}
