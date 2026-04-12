namespace TaskFlow.Application.Common.Models;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Description,
    string UserRole);
