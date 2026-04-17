using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Infrastructure.Persistence;

public class BoardRepository(AppDbContext context) : IBoardRepository
{
    // Board
    public Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Boards
            .Include(b => b.Columns)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Board?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => context.Boards
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(b => b.ProjectId == projectId, cancellationToken);

    public async Task AddAsync(Board board, CancellationToken cancellationToken = default)
    {
        await context.Boards.AddAsync(board, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    // Column
    public Task<Column?> GetColumnByIdAsync(Guid columnId, CancellationToken cancellationToken = default)
        => context.Columns
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == columnId, cancellationToken);

    public async Task AddColumnAsync(Column column, CancellationToken cancellationToken = default)
    {
        await context.Columns.AddAsync(column, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateColumnAsync(Column column, CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public Task<Guid?> GetProjectIdByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default)
        => context.Columns
            .Where(c => c.Id == columnId)
            .Join(context.Boards, c => c.BoardId, b => b.Id, (c, b) => (Guid?)b.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

    // Task
    public Task<BoardTask?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        => context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

    public async Task<IReadOnlyList<BoardTask>> GetTasksByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default)
        => await context.Tasks
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Order)
            .ToListAsync(cancellationToken);

    public async Task AddTaskAsync(BoardTask task, CancellationToken cancellationToken = default)
    {
        await context.Tasks.AddAsync(task, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateTaskAsync(BoardTask task, CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskByIdAsync(taskId, cancellationToken);
        if (task is not null)
        {
            context.Tasks.Remove(task);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<Guid?> GetProjectIdByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        => context.Tasks
            .Where(t => t.Id == taskId)
            .Join(context.Columns, t => t.ColumnId, c => c.Id, (t, c) => c)
            .Join(context.Boards, c => c.BoardId, b => b.Id, (c, b) => (Guid?)b.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
}
