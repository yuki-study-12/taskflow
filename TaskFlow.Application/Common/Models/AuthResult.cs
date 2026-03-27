namespace TaskFlow.Application.Common.Models;

public sealed class AuthResult
{
    public bool Succeeded { get; private init; }
    public string? Token { get; private init; }
    public UserDto? User { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    public static AuthResult Success(UserDto user, string token) =>
        new() { Succeeded = true, User = user, Token = token };

    public static AuthResult Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };
}
