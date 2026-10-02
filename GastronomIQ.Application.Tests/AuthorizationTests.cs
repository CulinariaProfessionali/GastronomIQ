using GastronomIQ.Application.Authorization;
using GastronomIQ.Infrastructure.Authorization;

namespace GastronomIQ.Application.Tests;

public class AuthorizationTests
{
    [Fact]
    public void Permission_is_granted_when_present()
    {
        var service = new AuthorizationService();
        var context = new AuthorizationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[] { PermissionCatalogue.RecipeRead });

        Assert.True(service.HasPermission(context, PermissionCatalogue.RecipeRead));
    }

    [Fact]
    public void Permission_is_denied_when_absent()
    {
        var service = new AuthorizationService();
        var context = new AuthorizationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[] { PermissionCatalogue.RecipeRead });

        Assert.False(service.HasPermission(context, PermissionCatalogue.RecipeManage));
    }

    [Fact]
    public void Organization_membership_is_granted_when_ids_match()
    {
        var service = new AuthorizationService();
        var organizationId = Guid.NewGuid();
        var context = new AuthorizationContext(
            Guid.NewGuid(),
            organizationId,
            Array.Empty<string>());

        Assert.True(service.BelongsToOrganization(context, organizationId));
    }

    [Fact]
    public void Organization_membership_is_denied_when_ids_differ()
    {
        var service = new AuthorizationService();
        var context = new AuthorizationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Array.Empty<string>());

        Assert.False(service.BelongsToOrganization(context, Guid.NewGuid()));
    }
}
