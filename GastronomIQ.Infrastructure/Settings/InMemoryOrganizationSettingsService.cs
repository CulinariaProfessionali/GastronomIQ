using System.Collections.Concurrent;
using System.Text.Json;
using GastronomIQ.Application.Settings;

namespace GastronomIQ.Infrastructure.Settings;

public sealed class InMemoryOrganizationSettingsService : IOrganizationSettingsService
{
    private readonly ConcurrentDictionary<string, object> _settings = new();

    public Task<SettingDto?> GetAsync(
        Guid organizationId,
        string key,
        CancellationToken cancellationToken)
    {
        var composite = $"{organizationId:N}:{key}";
        return Task.FromResult(
            _settings.TryGetValue(composite, out var value)
                ? new SettingDto(key, value)
                : null);
    }

    public Task SetAsync(
        Guid organizationId,
        string key,
        object value,
        CancellationToken cancellationToken)
    {
        _settings[$"{organizationId:N}:{key}"] = value;
        return Task.CompletedTask;
    }
}
