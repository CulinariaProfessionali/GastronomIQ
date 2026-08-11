namespace GastronomIQ.Application.Platform;

public sealed record TenantContext(
    Guid UserId,
    Guid OrganizationId,
    Guid? BranchId);

public sealed record IdempotencyRequest(
    string Key,
    string Fingerprint);

public sealed record IdempotencyRecord(
    string Key,
    string Fingerprint,
    int StatusCode,
    string ResponseBody,
    DateTimeOffset CreatedAt);

public sealed record AuditEvent(
    Guid Id,
    Guid OrganizationId,
    Guid? BranchId,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? BeforeJson,
    string? AfterJson,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public interface ITenantContextAccessor
{
    TenantContext? Current { get; }
}

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(
        string key,
        CancellationToken cancellationToken);

    Task SaveAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken);
}

public interface IAuditWriter
{
    Task WriteAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken);
}

public static class TenantGuard
{
    public static void EnsureOrganization(
        TenantContext context,
        Guid organizationId)
    {
        if (context.OrganizationId != organizationId)
            throw new UnauthorizedAccessException(
                "Organization context does not match authenticated tenant.");
    }

    public static void EnsureBranch(
        TenantContext context,
        Guid branchId)
    {
        if (context.BranchId.HasValue &&
            context.BranchId.Value != branchId)
            throw new UnauthorizedAccessException(
                "Branch context does not match authenticated branch.");
    }
}
