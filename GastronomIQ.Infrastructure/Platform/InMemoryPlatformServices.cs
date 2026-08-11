using System.Collections.Concurrent;
using GastronomIQ.Application.Platform;

namespace GastronomIQ.Infrastructure.Platform;

public sealed class InMemoryTenantContextAccessor : ITenantContextAccessor
{
    private readonly AsyncLocal<TenantContext?> _context = new();

    public TenantContext? Current => _context.Value;

    public void Set(TenantContext context) => _context.Value = context;

    public void Clear() => _context.Value = null;
}

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new();

    public Task<IdempotencyRecord?> GetAsync(
        string key,
        CancellationToken cancellationToken)
    {
        _records.TryGetValue(key, out var record);
        return Task.FromResult(record);
    }

    public Task SaveAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken)
    {
        _records[record.Key] = record;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAuditWriter : IAuditWriter
{
    private readonly ConcurrentBag<AuditEvent> _events = new();

    public Task WriteAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        _events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<AuditEvent> Events => _events.ToArray();
}

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken) =>
        action(cancellationToken);
}
