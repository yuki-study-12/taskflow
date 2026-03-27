using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Auth.Commands;

public sealed record LoginCommand(
    string Email,
    string Password);

public sealed class LoginCommandHandler(IIdentityService identityService)
    : ICommandHandler<LoginCommand, AuthResult>
{
    public Task<AuthResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.LoginAsync(
            command.Email,
            command.Password,
            cancellationToken);
}
