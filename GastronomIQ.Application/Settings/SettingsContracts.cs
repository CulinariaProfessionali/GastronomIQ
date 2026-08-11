namespace GastronomIQ.Application.Settings;

public sealed record SettingDto(string Key, object Value);

public interface IOrganizationSettingsService
{
    Task<SettingDto?> GetAsync(
        Guid organizationId,
        string key,
        CancellationToken cancellationToken);

    Task SetAsync(
        Guid organizationId,
        string key,
        object value,
        CancellationToken cancellationToken);
}
