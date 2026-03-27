using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Auth.Commands;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName);

public sealed class RegisterCommandHandler(IIdentityService identityService)
    : ICommandHandler<RegisterCommand, AuthResult>
{
    public Task<AuthResult> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.RegisterAsync(
            command.Email,
            command.Password,
            command.DisplayName,
            cancellationToken);
}
