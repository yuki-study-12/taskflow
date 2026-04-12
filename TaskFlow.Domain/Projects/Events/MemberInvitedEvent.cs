namespace TaskFlow.Domain.Projects.Events;

public record MemberInvitedEvent(
    Guid ProjectId,
    Guid UserId,
    MemberRole Role,
    DateTime OccurredOn
) : Common.IDomainEvent;
