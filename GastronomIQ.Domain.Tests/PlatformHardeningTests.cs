using GastronomIQ.Application.Platform;

namespace GastronomIQ.Domain.Tests;

public class PlatformHardeningTests
{
    [Fact]
    public void Tenant_guard_accepts_matching_organization()
    {
        var context = new TenantContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);

        TenantGuard.EnsureOrganization(
            context,
            context.OrganizationId);
    }

    [Fact]
    public void Tenant_guard_rejects_other_organization()
    {
        var context = new TenantContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);

        Assert.Throws<UnauthorizedAccessException>(() =>
            TenantGuard.EnsureOrganization(
                context,
                Guid.NewGuid()));
    }

    [Fact]
    public void Tenant_guard_rejects_other_branch()
    {
        var context = new TenantContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Throws<UnauthorizedAccessException>(() =>
            TenantGuard.EnsureBranch(
                context,
                Guid.NewGuid()));
    }
}
