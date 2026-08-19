namespace TaskFlow.Application.Common.Models;

public sealed record CommentDto(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    string AuthorDisplayName,
    string Body,
    DateTime CreatedAt);

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    Guid ColumnId,
    string Title,
    string Description,
    Guid? AssigneeId,
    int Order,
    DateTime? DueDate,
    TaskFlow.Domain.Boards.Priority Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ColumnDto(
    Guid Id,
    Guid BoardId,
    string Name,
    int Order,
    IReadOnlyList<TaskDto> Tasks);

public sealed record BoardDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    IReadOnlyList<ColumnDto> Columns);

public sealed record MyTaskDto(
    Guid Id,
    string Title,
    string Description,
    Guid ProjectId,
    string ProjectName,
    Guid ColumnId,
    string ColumnName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
