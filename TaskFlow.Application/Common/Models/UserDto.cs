namespace TaskFlow.Application.Common.Models;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl);
