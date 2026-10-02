namespace GastronomIQ.Domain.Identity;

public sealed record UserId(Guid Value);
public sealed record OrganizationId(Guid Value);
public sealed record BranchId(Guid Value);
public sealed record RoleId(Guid Value);
public sealed record PermissionId(Guid Value);

public sealed class User
{
    public UserId Id { get; init; } = new(Guid.NewGuid());
    public OrganizationId? OrganizationId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public User(string email, string displayName, OrganizationId? organizationId = null)
    {
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        OrganizationId = organizationId;
    }

    public void AssignOrganization(OrganizationId organizationId) => OrganizationId = organizationId;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
