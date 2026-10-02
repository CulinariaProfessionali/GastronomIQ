namespace GastronomIQ.Application.Billing;

public sealed record SubscriptionDto(
    Guid Id,
    Guid OrganizationId,
    string PlanCode,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt);

public interface IEntitlementService
{
    Task<bool> HasEntitlementAsync(
        Guid organizationId,
        string entitlement,
        CancellationToken cancellationToken);
}
