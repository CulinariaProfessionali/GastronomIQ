using System.Security.Claims;

namespace GastronomIQ.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    public static Guid GetOrganizationId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("organization_id"), out var id)
            ? id
            : Guid.Empty;

    public static IReadOnlyCollection<string> GetPermissions(this ClaimsPrincipal principal) =>
        principal.FindAll("permission")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
