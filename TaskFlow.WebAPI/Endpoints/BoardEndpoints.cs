using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Boards.Commands;
using TaskFlow.Application.Boards.Queries;
using TaskFlow.Application.Common.Constants;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Contracts.Boards;

namespace TaskFlow.WebAPI.Endpoints;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var projectGroup = app.MapGroup("/api/projects")
            .WithTags("Boards")
            .RequireAuthorization();

        projectGroup.MapGet("/{id:guid}/board", GetBoardByProjectIdAsync)
            .WithName("GetBoardByProjectId");

        projectGroup.MapPost("/{id:guid}/boards", CreateBoardAsync)
            .WithName("CreateBoard");

        var boardGroup = app.MapGroup("/api/boards")
            .WithTags("Boards")
            .RequireAuthorization();

        boardGroup.MapPost("/{id:guid}/columns", CreateColumnAsync)
            .WithName("CreateColumn");

        var columnGroup = app.MapGroup("/api/columns")
            .WithTags("Boards")
            .RequireAuthorization();

        columnGroup.MapPut("/{id:guid}", UpdateColumnAsync)
            .WithName("UpdateColumn");

        columnGroup.MapPost("/{id:guid}/tasks", CreateTaskAsync)
            .WithName("CreateTask");

        var taskGroup = app.MapGroup("/api/tasks")
            .WithTags("Boards")
            .RequireAuthorization();

        taskGroup.MapGet("/{id:guid}", GetTaskByIdAsync)
            .WithName("GetTaskById");

        taskGroup.MapPut("/{id:guid}", UpdateTaskAsync)
            .WithName("UpdateTask");

        taskGroup.MapDelete("/{id:guid}", DeleteTaskAsync)
            .WithName("DeleteTask");

        return app;
    }

    private static async Task<Results<Ok<BoardResponse>, UnauthorizedHttpResult>>
        GetBoardByProjectIdAsync(
            Guid id,
            ClaimsPrincipal principal,
            IQueryHandler<GetBoardByProjectIdQuery, BoardDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var board = await handler.HandleAsync(new GetBoardByProjectIdQuery(id, userId), cancellationToken);

        var response = ToResponse(board);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<BoardResponse>, UnauthorizedHttpResult>>
        CreateBoardAsync(
            Guid id,
            [FromBody] CreateBoardRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<CreateBoardCommand, BoardDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var board = await handler.HandleAsync(new CreateBoardCommand(id, request.Name, userId), cancellationToken);

        var response = ToResponse(board);
        return TypedResults.Created($"/api/projects/{id}/board", response);
    }

    private static async Task<Results<Created<ColumnResponse>, UnauthorizedHttpResult>>
        CreateColumnAsync(
            Guid id,
            [FromBody] CreateColumnRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<CreateColumnCommand, ColumnDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var column = await handler.HandleAsync(new CreateColumnCommand(id, request.Name, userId), cancellationToken);

        var response = ToResponse(column);
        return TypedResults.Created($"/api/boards/{id}/columns/{column.Id}", response);
    }

    private static async Task<Results<Ok<ColumnResponse>, UnauthorizedHttpResult>>
        UpdateColumnAsync(
            Guid id,
            [FromBody] UpdateColumnRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<UpdateColumnCommand, ColumnDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var column = await handler.HandleAsync(new UpdateColumnCommand(id, request.Name, userId), cancellationToken);

        return TypedResults.Ok(ToResponse(column));
    }

    private static async Task<Results<Created<TaskResponse>, UnauthorizedHttpResult>>
        CreateTaskAsync(
            Guid id,
            [FromBody] CreateTaskRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<CreateTaskCommand, TaskDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var task = await handler.HandleAsync(
            new CreateTaskCommand(id, request.Title, request.Description, request.AssigneeId, userId),
            cancellationToken);

        var response = ToResponse(task);
        return TypedResults.Created($"/api/tasks/{task.Id}", response);
    }

    private static async Task<Results<Ok<TaskResponse>, UnauthorizedHttpResult>>
        GetTaskByIdAsync(
            Guid id,
            ClaimsPrincipal principal,
            IQueryHandler<GetTaskByIdQuery, TaskDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var task = await handler.HandleAsync(new GetTaskByIdQuery(id, userId), cancellationToken);

        return TypedResults.Ok(ToResponse(task));
    }

    private static async Task<Results<Ok<TaskResponse>, UnauthorizedHttpResult>>
        UpdateTaskAsync(
            Guid id,
            [FromBody] UpdateTaskRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<UpdateTaskCommand, TaskDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var task = await handler.HandleAsync(
            new UpdateTaskCommand(id, request.Title, request.Description, request.AssigneeId, userId),
            cancellationToken);

        return TypedResults.Ok(ToResponse(task));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>>
        DeleteTaskAsync(
            Guid id,
            ClaimsPrincipal principal,
            ICommandHandler<DeleteTaskCommand, bool> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        await handler.HandleAsync(new DeleteTaskCommand(id, userId), cancellationToken);

        return TypedResults.NoContent();
    }

    private static BoardResponse ToResponse(BoardDto dto) =>
        new(dto.Id, dto.ProjectId, dto.Name,
            dto.Columns.Select(ToResponse).ToList());

    private static ColumnResponse ToResponse(ColumnDto dto) =>
        new(dto.Id, dto.BoardId, dto.Name, dto.Order,
            dto.Tasks.Select(ToResponse).ToList());

    private static TaskResponse ToResponse(TaskDto dto) =>
        new(dto.Id, dto.ColumnId, dto.Title, dto.Description, dto.AssigneeId, dto.Order, dto.CreatedAt, dto.UpdatedAt);

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var idClaim = principal.FindFirstValue(AppClaimTypes.UserId);
        return Guid.TryParse(idClaim, out userId);
    }
}
