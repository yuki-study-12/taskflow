using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Application.Boards.Commands;

public sealed record AddCommentCommand(Guid TaskId, string Body, Guid UserId);

public sealed class AddCommentCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository,
    IIdentityService identityService)
    : ICommandHandler<AddCommentCommand, CommentDto>
{
    public async Task<CommentDto> HandleAsync(
        AddCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var task = await boardRepository.GetTaskWithCommentsAsync(command.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", command.TaskId);

        var projectId = await boardRepository.GetProjectIdByTaskIdAsync(command.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", command.TaskId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        if (!project.Members.Any(m => m.UserId == command.UserId))
            throw new ForbiddenAccessException();

        var comment = task.AddComment(command.UserId, command.Body);
        await boardRepository.AddCommentAsync(comment, cancellationToken);

        var author = await identityService.GetUserByIdAsync(comment.AuthorId, cancellationToken);
        return new CommentDto(comment.Id, comment.TaskId, comment.AuthorId, author?.DisplayName ?? "不明なユーザー", comment.Body, comment.CreatedAt);
    }
}

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}
