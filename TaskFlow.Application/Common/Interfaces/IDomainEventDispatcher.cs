using TaskFlow.Domain.Common;

namespace TaskFlow.Application.Common.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
