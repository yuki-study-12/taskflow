namespace TaskFlow.Contracts.Boards;

public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    Guid? AssigneeId,
    DateTime? DueDate,
    string Priority);
