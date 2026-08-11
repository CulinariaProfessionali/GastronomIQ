namespace GastronomIQ.Application.Platform;

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
