using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth.Commands;
using TaskFlow.Application.Auth.Queries;
using TaskFlow.Application.Common.Constants;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Contracts.Auth;

namespace TaskFlow.WebAPI.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
             .WithName("Register")
             .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
             .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
             .WithName("Login")
             .AddEndpointFilter<ValidationFilter<LoginRequest>>()
             .AllowAnonymous();

        group.MapGet("/me", GetMeAsync)
             .WithName("GetCurrentUser")
             .RequireAuthorization();

        return group;
    }

    private static async Task<Results<Ok<AuthResponse>, BadRequest<IReadOnlyList<string>>>>
        RegisterAsync(
            [FromBody] RegisterRequest request,
            ICommandHandler<RegisterCommand, AuthResult> handler,
            CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterCommand(request.Email, request.Password, request.DisplayName),
            cancellationToken);

        if (!result.Succeeded)
            return TypedResults.BadRequest(result.Errors);

        return TypedResults.Ok(new AuthResponse(
            result.User!.Id,
            result.User.Email,
            result.User.DisplayName,
            result.Token!));
    }

    private static async Task<Results<Ok<AuthResponse>, BadRequest<IReadOnlyList<string>>>>
        LoginAsync(
            [FromBody] LoginRequest request,
            ICommandHandler<LoginCommand, AuthResult> handler,
            CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
            return TypedResults.BadRequest(result.Errors);

        return TypedResults.Ok(new AuthResponse(
            result.User!.Id,
            result.User.Email,
            result.User.DisplayName,
            result.Token!));
    }

    private static async Task<Results<Ok<UserProfileResponse>, UnauthorizedHttpResult, NotFound>>
        GetMeAsync(
            ClaimsPrincipal principal,
            IQueryHandler<GetCurrentUserQuery, UserDto?> handler,
            CancellationToken cancellationToken)
    {
        var idClaim = principal.FindFirstValue(AppClaimTypes.UserId);
        if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
            return TypedResults.Unauthorized();

        var user = await handler.HandleAsync(
            new GetCurrentUserQuery(userId),
            cancellationToken);

        if (user is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new UserProfileResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl));
    }
}
