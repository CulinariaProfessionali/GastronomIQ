namespace GastronomIQ.Api.Configuration;

public sealed class ProductionConfiguration
{
    public string Environment { get; init; } = "Development";
    public string DatabaseProvider { get; init; } = "PostgreSQL";
    public string DatabaseConnectionString { get; init; } = string.Empty;
    public bool RequireHttps { get; init; } = true;
    public bool EnableSwagger { get; init; } = false;
    public int MinimumSchemaVersion { get; init; } = 11;
}
