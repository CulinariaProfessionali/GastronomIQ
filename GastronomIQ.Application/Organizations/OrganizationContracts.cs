namespace GastronomIQ.Application.Organizations;

public sealed record CreateOrganizationRequest(string Name, string Code);
public sealed record CreateBranchRequest(Guid OrganizationId, string Name, string Code);

public sealed record OrganizationDto(Guid Id, string Name, string Code);
public sealed record BranchDto(Guid Id, Guid OrganizationId, string Name, string Code);
