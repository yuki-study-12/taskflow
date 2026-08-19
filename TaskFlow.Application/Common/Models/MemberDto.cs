namespace TaskFlow.Application.Common.Models;

public sealed record MemberDto(
    Guid UserId,
    string Role,
    string DisplayName,
    string? AvatarUrl);
