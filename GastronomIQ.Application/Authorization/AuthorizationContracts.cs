namespace GastronomIQ.Application.Authorization;

public sealed record AuthorizationContext(
    Guid UserId,
    Guid OrganizationId,
    IReadOnlyCollection<string> Permissions);

public interface IAuthorizationService
{
    bool HasPermission(AuthorizationContext context, string permission);
    bool BelongsToOrganization(AuthorizationContext context, Guid organizationId);
}
