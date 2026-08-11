using GastronomIQ.Application.Billing;

namespace GastronomIQ.Infrastructure.Billing;

public sealed class InMemoryEntitlementService : IEntitlementService
{
    public Task<bool> HasEntitlementAsync(
        Guid organizationId,
        string entitlement,
        CancellationToken cancellationToken)
    {
        // Development baseline. Replace with persisted subscription evaluation.
        return Task.FromResult(true);
    }
}
