using FluentValidation;
using TaskFlow.Application.Common.Interfaces;
using ValidationException = TaskFlow.Application.Common.Exceptions.ValidationException;

namespace TaskFlow.Application.Common.Behaviors;

internal sealed class ValidationBehavior<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResult>
    where TCommand : notnull
{
    public async Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!validators.Any())
            return await inner.HandleAsync(command, cancellationToken);

        var context = new ValidationContext<TCommand>(command);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        return await inner.HandleAsync(command, cancellationToken);
    }
}
