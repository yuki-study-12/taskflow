using TaskFlow.Domain.Common;

namespace TaskFlow.Application.Common.Interfaces;

public interface INotificationHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
