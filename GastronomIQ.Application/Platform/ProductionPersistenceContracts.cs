namespace GastronomIQ.Application.Platform;

public interface IRepository<TEntity, in TId> where TEntity : class
{
    Task<TEntity?> GetAsync(TId id, CancellationToken cancellationToken);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);
}

public interface ITransactionManager
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}

public interface IMigrationRunner
{
    Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken);
    Task ApplyPendingAsync(CancellationToken cancellationToken);
}

public sealed record DatabaseReadiness(
    bool IsReady,
    int SchemaVersion,
    string Provider,
    string? Detail);
