using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Contracts.Auth;

public sealed record LoginRequest(
    [property: Required, EmailAddress]
    string Email,

    [property: Required]
    string Password);
