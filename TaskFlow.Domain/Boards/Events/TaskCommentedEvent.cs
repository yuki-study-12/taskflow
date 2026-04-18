namespace TaskFlow.Domain.Boards.Events;

public record TaskCommentedEvent(
    Guid TaskId,
    Guid CommentId,
    Guid AuthorId,
    DateTime OccurredOn
) : Common.IDomainEvent;
