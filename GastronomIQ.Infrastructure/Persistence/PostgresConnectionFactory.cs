using Npgsql;

namespace GastronomIQ.Infrastructure.Persistence;

public sealed class PostgresConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("PostgreSQL connection string is required.");
        _connectionString = connectionString;
    }

    public NpgsqlConnection Create() => new(_connectionString);
}
