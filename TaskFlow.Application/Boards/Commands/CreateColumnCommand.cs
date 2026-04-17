using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Application.Boards.Commands;

public sealed record CreateColumnCommand(
    Guid BoardId,
    string Name,
    Guid UserId);

public sealed class CreateColumnCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : ICommandHandler<CreateColumnCommand, ColumnDto>
{
    public async Task<ColumnDto> HandleAsync(
        CreateColumnCommand command,
        CancellationToken cancellationToken = default)
    {
        var board = await boardRepository.GetByIdAsync(command.BoardId, cancellationToken)
            ?? throw new NotFoundException("Board", command.BoardId);

        var project = await projectRepository.GetByIdAsync(board.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", board.ProjectId);

        if (!project.Members.Any(m => m.UserId == command.UserId))
            throw new ForbiddenAccessException();

        var order = board.Columns.Count;
        var column = Column.Create(command.BoardId, command.Name, order);
        await boardRepository.AddColumnAsync(column, cancellationToken);

        return new ColumnDto(column.Id, column.BoardId, column.Name, column.Order, []);
    }
}

public sealed class CreateColumnCommandValidator : AbstractValidator<CreateColumnCommand>
{
    public CreateColumnCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
