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
}
