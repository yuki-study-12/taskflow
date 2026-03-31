using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Projects;

namespace TaskFlow.Infrastructure.Persistence;

public class ProjectRepository(AppDbContext context) : IProjectRepository
{
    
    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Projects
            .Include(p => p.Members)
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);
    }
    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await context.Projects.AddAsync(project, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        context.Projects.Update(project);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await GetByIdAsync(id, cancellationToken);
        if (project is not null)
        {
            context.Projects.Remove(project);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}