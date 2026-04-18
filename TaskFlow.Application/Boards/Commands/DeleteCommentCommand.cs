using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Commands;

public sealed record DeleteCommentCommand(Guid CommentId, Guid UserId);

public sealed class DeleteCommentCommandHandler(
    IBoardRepository boardRepository)
    : ICommandHandler<DeleteCommentCommand, bool>
{
    public async Task<bool> HandleAsync(
        DeleteCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var comment = await boardRepository.GetCommentByIdAsync(command.CommentId, cancellationToken)
            ?? throw new NotFoundException("Comment", command.CommentId);

        if (comment.AuthorId != command.UserId)
            throw new ForbiddenAccessException();

        await boardRepository.DeleteCommentAsync(command.CommentId, cancellationToken);
        return true;
    }
}
