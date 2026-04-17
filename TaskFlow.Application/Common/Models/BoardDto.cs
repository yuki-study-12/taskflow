namespace TaskFlow.Application.Common.Models;

public sealed record TaskDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    string Description,
    Guid? AssigneeId,
    int Order,
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
