namespace TaskFlow.Contracts.Boards;

public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    Guid? AssigneeId);
