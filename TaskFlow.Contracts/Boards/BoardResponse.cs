namespace TaskFlow.Contracts.Boards;

public sealed record TaskResponse(
    Guid Id,
    Guid ColumnId,
    string Title,
    string Description,
    Guid? AssigneeId,
    int Order,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ColumnResponse(
    Guid Id,
    Guid BoardId,
    string Name,
    int Order,
    IReadOnlyList<TaskResponse> Tasks);

public sealed record BoardResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    IReadOnlyList<ColumnResponse> Columns);
