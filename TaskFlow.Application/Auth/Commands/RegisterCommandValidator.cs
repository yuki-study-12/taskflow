using FluentValidation;

namespace TaskFlow.Application.Auth.Commands;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(50);
    }
}
