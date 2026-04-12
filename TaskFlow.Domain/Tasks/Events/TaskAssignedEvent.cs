namespace TaskFlow.Domain.Tasks.Events;

public record TaskAssignedEvent(
    Guid TaskId,
    Guid AssigneeId,
    DateTime OccurredOn
) : Common.IDomainEvent;
