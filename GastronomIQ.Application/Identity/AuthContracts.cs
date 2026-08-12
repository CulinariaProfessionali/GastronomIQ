namespace GastronomIQ.Application.Identity;

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string OrganizationName);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);

public enum RegistrationStatus
{
    Success,
    InvalidRequest,
    EmailAlreadyExists
}

public sealed record RegistrationResult(
    RegistrationStatus Status,
    TokenResponse? Tokens);

public interface IAuthenticationService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<TokenResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    TokenResponse CreateToken(Guid userId, Guid organizationId, IEnumerable<string> permissions);
}
