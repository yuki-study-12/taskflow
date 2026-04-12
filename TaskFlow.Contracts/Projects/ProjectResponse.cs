namespace TaskFlow.Contracts.Projects;

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string Description,
    string UserRole);
