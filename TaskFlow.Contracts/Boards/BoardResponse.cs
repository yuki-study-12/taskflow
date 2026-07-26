namespace TaskFlow.Contracts.Boards;

public sealed record CommentResponse(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    string AuthorDisplayName,
    string Body,
    DateTime CreatedAt);

public sealed record TaskResponse(
    Guid Id,
    Guid ColumnId,
    string Title,
    string Description,
    Guid? AssigneeId,
    int Order,
    DateTime? DueDate,
    string Priority,
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

public sealed record MyTaskResponse(
    Guid Id,
    string Title,
    string Description,
    Guid ProjectId,
    string ProjectName,
    Guid ColumnId,
    string ColumnName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
