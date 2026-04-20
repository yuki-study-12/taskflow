using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;

namespace TaskFlow.Infrastructure;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(domainEvent.GetType());
            var handler = serviceProvider.GetService(handlerType);
            if (handler is null) continue;

            var method = handlerType.GetMethod(nameof(INotificationHandler<IDomainEvent>.HandleAsync))!;
            await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
        }
    }
}
