namespace TaskFlow.Contracts.Auth;

public sealed record UserProfileResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl);
