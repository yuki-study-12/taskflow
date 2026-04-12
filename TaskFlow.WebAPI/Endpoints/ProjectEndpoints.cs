using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common.Constants;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Application.Projects.Queries;
using TaskFlow.Contracts.Projects;

namespace TaskFlow.WebAPI.Endpoints;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        group.MapGet("/", GetProjectsAsync)
             .WithName("GetProjects");

        group.MapGet("/{id:guid}", GetProjectByIdAsync)
             .WithName("GetProjectById");

        group.MapPost("/", CreateProjectAsync)
             .WithName("CreateProject");

        group.MapPut("/{id:guid}", UpdateProjectAsync)
             .WithName("UpdateProject");

        group.MapDelete("/{id:guid}", DeleteProjectAsync)
             .WithName("DeleteProject");

        group.MapGet("/{id:guid}/members", GetProjectMembersAsync)
             .WithName("GetProjectMembers");

        group.MapPost("/{id:guid}/members", InviteMemberAsync)
             .WithName("InviteMember");

        group.MapPut("/{id:guid}/members/{uid:guid}", UpdateMemberRoleAsync)
             .WithName("UpdateMemberRole");

        group.MapDelete("/{id:guid}/members/{uid:guid}", RemoveMemberAsync)
             .WithName("RemoveMember");

        return group;
    }

    private static async Task<Results<Ok<IReadOnlyList<ProjectResponse>>, UnauthorizedHttpResult>>
        GetProjectsAsync(
            ClaimsPrincipal principal,
            IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectDto>> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var projects = await handler.HandleAsync(new GetProjectsQuery(userId), cancellationToken);

        var response = projects
            .Select(p => new ProjectResponse(p.Id, p.Name, p.Description, p.UserRole))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<ProjectResponse>>(response);
    }

    private static async Task<Results<Ok<ProjectResponse>, UnauthorizedHttpResult>>
        GetProjectByIdAsync(
            Guid id,
            ClaimsPrincipal principal,
            IQueryHandler<GetProjectByIdQuery, ProjectDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var project = await handler.HandleAsync(new GetProjectByIdQuery(id, userId), cancellationToken);

        return TypedResults.Ok(new ProjectResponse(project.Id, project.Name, project.Description, project.UserRole));
    }

    private static async Task<Results<Created<ProjectResponse>, UnauthorizedHttpResult>>
        CreateProjectAsync(
            [FromBody] CreateProjectRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<CreateProjectCommand, ProjectDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var project = await handler.HandleAsync(
            new CreateProjectCommand(request.Name, request.Description, userId),
            cancellationToken);

        var response = new ProjectResponse(project.Id, project.Name, project.Description, project.UserRole);
        return TypedResults.Created($"/api/projects/{project.Id}", response);
    }

    private static async Task<Results<Ok<ProjectResponse>, UnauthorizedHttpResult>>
        UpdateProjectAsync(
            Guid id,
            [FromBody] UpdateProjectRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<UpdateProjectCommand, ProjectDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var project = await handler.HandleAsync(
            new UpdateProjectCommand(id, request.Name, request.Description, userId),
            cancellationToken);

        return TypedResults.Ok(new ProjectResponse(project.Id, project.Name, project.Description, project.UserRole));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>>
        DeleteProjectAsync(
            Guid id,
            ClaimsPrincipal principal,
            ICommandHandler<DeleteProjectCommand, bool> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        await handler.HandleAsync(new DeleteProjectCommand(id, userId), cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<IReadOnlyList<MemberResponse>>, UnauthorizedHttpResult>>
        GetProjectMembersAsync(
            Guid id,
            ClaimsPrincipal principal,
            IQueryHandler<GetProjectMembersQuery, IReadOnlyList<MemberDto>> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var members = await handler.HandleAsync(new GetProjectMembersQuery(id, userId), cancellationToken);

        var response = members
            .Select(m => new MemberResponse(m.UserId, m.Role))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<MemberResponse>>(response);
    }

    private static async Task<Results<Created<MemberResponse>, UnauthorizedHttpResult>>
        InviteMemberAsync(
            Guid id,
            [FromBody] InviteMemberRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<InviteMemberCommand, MemberDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var member = await handler.HandleAsync(
            new InviteMemberCommand(id, userId, request.UserId, request.Role),
            cancellationToken);

        var response = new MemberResponse(member.UserId, member.Role);
        return TypedResults.Created($"/api/projects/{id}/members/{member.UserId}", response);
    }

    private static async Task<Results<Ok<MemberResponse>, UnauthorizedHttpResult>>
        UpdateMemberRoleAsync(
            Guid id,
            Guid uid,
            [FromBody] UpdateMemberRoleRequest request,
            ClaimsPrincipal principal,
            ICommandHandler<UpdateMemberRoleCommand, MemberDto> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        var member = await handler.HandleAsync(
            new UpdateMemberRoleCommand(id, userId, uid, request.Role),
            cancellationToken);

        return TypedResults.Ok(new MemberResponse(member.UserId, member.Role));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>>
        RemoveMemberAsync(
            Guid id,
            Guid uid,
            ClaimsPrincipal principal,
            ICommandHandler<RemoveMemberCommand, bool> handler,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return TypedResults.Unauthorized();

        await handler.HandleAsync(new RemoveMemberCommand(id, userId, uid), cancellationToken);

        return TypedResults.NoContent();
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var idClaim = principal.FindFirstValue(AppClaimTypes.UserId);
        return Guid.TryParse(idClaim, out userId);
    }
}
