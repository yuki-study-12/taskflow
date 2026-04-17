using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Application.Boards.Commands;

public sealed record CreateBoardCommand(
    Guid ProjectId,
    string Name,
    Guid UserId);

public sealed class CreateBoardCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : ICommandHandler<CreateBoardCommand, BoardDto>
{
    public async Task<BoardDto> HandleAsync(
        CreateBoardCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", command.ProjectId);

        if (!project.Members.Any(m => m.UserId == command.UserId))
            throw new ForbiddenAccessException();

        var board = Board.Create(command.ProjectId, command.Name);
        await boardRepository.AddAsync(board, cancellationToken);

        return new BoardDto(board.Id, board.ProjectId, board.Name, []);
    }
}

public sealed class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
