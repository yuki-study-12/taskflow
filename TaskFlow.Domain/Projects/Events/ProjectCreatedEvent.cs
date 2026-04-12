namespace TaskFlow.Domain.Projects.Events;

public record ProjectCreatedEvent(
    Guid ProjectId,
    Guid OwnerId,
    DateTime OccurredOn
) : Common.IDomainEvent;
