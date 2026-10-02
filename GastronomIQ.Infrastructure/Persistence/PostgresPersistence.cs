using GastronomIQ.Application.Platform;
using Npgsql;

namespace GastronomIQ.Infrastructure.Persistence;

public sealed class PostgresTransactionManager : ITransactionManager
{
    private readonly PostgresConnectionFactory _factory;
    public PostgresTransactionManager(PostgresConnectionFactory factory) => _factory = factory;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await ExecuteAsync(async ct => { await operation(ct); return true; }, cancellationToken);
    }
}

public sealed class PostgresMigrationRunner : IMigrationRunner
{
    private readonly PostgresConnectionFactory _factory;
    private readonly string _directory;
    public PostgresMigrationRunner(PostgresConnectionFactory factory, string directory)
    {
        _factory = factory;
        _directory = directory;
    }

    public async Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await EnsureTableAsync(connection, null, cancellationToken);
        await using var command = new NpgsqlCommand("SELECT COALESCE(MAX(version), 0) FROM schema_migrations;", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task ApplyPendingAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_directory))
            throw new DirectoryNotFoundException(_directory);

        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await EnsureTableAsync(connection, null, cancellationToken);

        foreach (var file in Directory.GetFiles(_directory, "*.sql").OrderBy(Path.GetFileName))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!int.TryParse(name.Split('_', 2)[0], out var version) || version == 0)
                continue;

            await using var check = new NpgsqlCommand("SELECT 1 FROM schema_migrations WHERE version = @version;", connection);
            check.Parameters.AddWithValue("version", version);
            if (await check.ExecuteScalarAsync(cancellationToken) is not null)
                continue;

            var sql = await File.ReadAllTextAsync(file, cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var command = new NpgsqlCommand(sql, connection, tx);
                await command.ExecuteNonQueryAsync(cancellationToken);
                await using var record = new NpgsqlCommand("INSERT INTO schema_migrations(version, description) VALUES (@version, @description);", connection, tx);
                record.Parameters.AddWithValue("version", version);
                record.Parameters.AddWithValue("description", name);
                await record.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private static async Task EnsureTableAsync(NpgsqlConnection connection, NpgsqlTransaction? tx, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version integer PRIMARY KEY,
                description varchar(250) NOT NULL,
                applied_at timestamptz NOT NULL DEFAULT now()
            );
            """, connection, tx);
        await command.ExecuteNonQueryAsync(ct);
    }
}

public sealed class PostgresIdempotencyStore : IIdempotencyStore
{
    private readonly PostgresConnectionFactory _factory;
    public PostgresIdempotencyStore(PostgresConnectionFactory factory) => _factory = factory;

    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT key, fingerprint, status_code, response_body::text, created_at FROM idempotency_records WHERE key=@key;", connection);
        command.Parameters.AddWithValue("key", key);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new IdempotencyRecord(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3), reader.GetFieldValue<DateTimeOffset>(4));
    }

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            INSERT INTO idempotency_records(key, fingerprint, status_code, response_body)
            VALUES (@key, @fingerprint, @status_code, @response_body::jsonb)
            ON CONFLICT (key) DO UPDATE SET fingerprint=EXCLUDED.fingerprint, status_code=EXCLUDED.status_code, response_body=EXCLUDED.response_body;
            """, connection);
        command.Parameters.AddWithValue("key", record.Key);
        command.Parameters.AddWithValue("fingerprint", record.Fingerprint);
        command.Parameters.AddWithValue("status_code", record.StatusCode);
        command.Parameters.AddWithValue("response_body", record.ResponseBody);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class PostgresAuditWriter : IAuditWriter
{
    private readonly PostgresConnectionFactory _factory;
    public PostgresAuditWriter(PostgresConnectionFactory factory) => _factory = factory;

    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            INSERT INTO audit_events
            (id, organization_id, branch_id, actor_user_id, action, entity_type, entity_id, before_json, after_json, correlation_id, occurred_at)
            VALUES (@id, @organization_id, @branch_id, @actor_user_id, @action, @entity_type, @entity_id, @before_json::jsonb, @after_json::jsonb, @correlation_id, @occurred_at);
            """, connection);
        command.Parameters.AddWithValue("id", auditEvent.Id);
        command.Parameters.AddWithValue("organization_id", auditEvent.OrganizationId);
        command.Parameters.AddWithValue("branch_id", (object?)auditEvent.BranchId ?? DBNull.Value);
        command.Parameters.AddWithValue("actor_user_id", (object?)auditEvent.ActorUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("action", auditEvent.Action);
        command.Parameters.AddWithValue("entity_type", auditEvent.EntityType);
        command.Parameters.AddWithValue("entity_id", (object?)auditEvent.EntityId ?? DBNull.Value);
        command.Parameters.AddWithValue("before_json", (object?)auditEvent.BeforeJson ?? DBNull.Value);
        command.Parameters.AddWithValue("after_json", (object?)auditEvent.AfterJson ?? DBNull.Value);
        command.Parameters.AddWithValue("correlation_id", auditEvent.CorrelationId);
        command.Parameters.AddWithValue("occurred_at", auditEvent.OccurredAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
