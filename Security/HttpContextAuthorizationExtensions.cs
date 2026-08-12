using GastronomIQ.Application.Authorization;

namespace GastronomIQ.Api.Security;

public static class HttpContextAuthorizationExtensions
{
    public static IResult? RequirePermission(
        this HttpContext http,
        IAuthorizationService authorization,
        string permission,
        out AuthorizationContext context)
    {
        context = new AuthorizationContext(
            http.User.GetUserId(),
            http.User.GetOrganizationId(),
            http.User.GetPermissions());

        if (context.UserId == Guid.Empty || context.OrganizationId == Guid.Empty)
            return Results.Unauthorized();

        return authorization.HasPermission(context, permission)
            ? null
            : Results.Forbid();
    }

    public static IResult? RequireOrganizationScope(
        this IAuthorizationService authorization,
        AuthorizationContext context,
        Guid organizationId)
        => authorization.BelongsToOrganization(context, organizationId)
            ? null
            : Results.Forbid();
}
