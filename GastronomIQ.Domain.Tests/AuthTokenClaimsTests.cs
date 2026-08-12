using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GastronomIQ.Api.Security;
using GastronomIQ.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;

namespace GastronomIQ.Domain.Tests;

public class AuthTokenClaimsTests
{
    [Fact]
    public void Jwt_token_contains_subject_organization_and_permission_claims()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var permissions = new[] { "recipe.read", "recipe.manage" };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "unit-test-signing-key-123456789012345",
                ["Jwt:AccessTokenMinutes"] = "30"
            })
            .Build();

        var service = new JwtTokenService(configuration);
        var token = service.CreateToken(userId, organizationId, permissions);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(organizationId.ToString(), jwt.Claims.First(c => c.Type == "organization_id").Value);
        Assert.Equal(permissions, jwt.Claims.Where(c => c.Type == "permission").Select(c => c.Value));
    }

    [Fact]
    public void Claims_principal_extensions_extract_org_and_deduplicate_permissions()
    {
        var organizationId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("organization_id", organizationId.ToString()),
            new Claim("permission", "recipe.read"),
            new Claim("permission", "RECIPE.READ"),
            new Claim("permission", "recipe.manage")
        ]));

        Assert.Equal(organizationId, principal.GetOrganizationId());
        Assert.Equal(2, principal.GetPermissions().Count);
    }
}
