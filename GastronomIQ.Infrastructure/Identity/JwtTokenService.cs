using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GastronomIQ.Application.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace GastronomIQ.Infrastructure.Identity;

public sealed class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public TokenResponse CreateToken(Guid userId, Guid organizationId, IEnumerable<string> permissions)
    {
        var key = _configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        var expires = DateTimeOffset.UtcNow.AddMinutes(
            _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 30);
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("organization_id", organizationId.ToString())
        };

        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            Guid.NewGuid().ToString("N"),
            expires);
    }
}
