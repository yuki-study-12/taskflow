using FluentValidation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Boards.Commands;

public sealed record UpdateColumnCommand(
    Guid ColumnId,
    string Name,
    Guid UserId);

public sealed class UpdateColumnCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository)
    : ICommandHandler<UpdateColumnCommand, ColumnDto>
{
    public async Task<ColumnDto> HandleAsync(
        UpdateColumnCommand command,
        CancellationToken cancellationToken = default)
    {
        var column = await boardRepository.GetColumnByIdAsync(command.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", command.ColumnId);

        var projectId = await boardRepository.GetProjectIdByColumnIdAsync(command.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", command.ColumnId);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        if (!project.Members.Any(m => m.UserId == command.UserId))
            throw new ForbiddenAccessException();

        column.Update(command.Name);
        await boardRepository.UpdateColumnAsync(column, cancellationToken);

        var tasks = column.Tasks
            .Select(t => new TaskDto(t.Id, projectId, t.ColumnId, t.Title, t.Description, t.AssigneeId, t.Order, t.DueDate, t.Priority, t.CreatedAt, t.UpdatedAt))
            .ToList();

        return new ColumnDto(column.Id, column.BoardId, column.Name, column.Order, tasks);
    }
}

public sealed class UpdateColumnCommandValidator : AbstractValidator<UpdateColumnCommand>
{
    public UpdateColumnCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
