namespace TaskFlow.Contracts.Boards;

public sealed record MoveTaskRequest(
    Guid TargetColumnId,
    int NewOrder);
