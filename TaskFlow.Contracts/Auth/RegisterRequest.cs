using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Contracts.Auth;

public sealed record RegisterRequest(
    [property: Required, EmailAddress]
    string Email,

    [property: Required, StringLength(100, MinimumLength = 8)]
    string Password,

    [property: Required, StringLength(50, MinimumLength = 1)]
    string DisplayName);
