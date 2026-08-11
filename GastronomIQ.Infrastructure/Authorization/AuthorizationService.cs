using GastronomIQ.Application.Authorization;

namespace GastronomIQ.Infrastructure.Authorization;

public sealed class AuthorizationService : IAuthorizationService
{
    public bool HasPermission(AuthorizationContext context, string permission) =>
        context.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool BelongsToOrganization(AuthorizationContext context, Guid organizationId) =>
        context.OrganizationId == organizationId;
}
