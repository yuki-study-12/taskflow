namespace TaskFlow.Domain.Tasks.Events;

public record TaskCreatedEvent(
    Guid TaskId,
    string Title,
    Guid ColumnId,
    Guid CreatorId,
    DateTime OccurredOn
) : Common.IDomainEvent;
