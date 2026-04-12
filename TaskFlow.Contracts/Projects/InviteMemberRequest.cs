namespace TaskFlow.Contracts.Projects;

public sealed record InviteMemberRequest(
    Guid UserId,
    string Role);
