namespace TaskFlow.Contracts.Boards;

public sealed record CreateTaskRequest(
    string Title,
    string Description,
    Guid? AssigneeId);
