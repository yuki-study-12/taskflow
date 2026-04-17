namespace TaskFlow.Domain.Tasks.Events;

public record TaskMovedEvent(
    Guid TaskId,
    Guid NewColumnId,
    int NewOrder,
    DateTime OccurredOn
) : Common.IDomainEvent;
