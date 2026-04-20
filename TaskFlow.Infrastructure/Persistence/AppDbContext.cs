using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IServiceProvider? serviceProvider = null)
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<TaskFlow.Domain.Projects.Project> Projects => Set<TaskFlow.Domain.Projects.Project>();
    public DbSet<TaskFlow.Domain.Boards.Board> Boards => Set<TaskFlow.Domain.Boards.Board>();
    public DbSet<TaskFlow.Domain.Boards.Column> Columns => Set<TaskFlow.Domain.Boards.Column>();
    public DbSet<TaskFlow.Domain.Boards.BoardTask> Tasks => Set<TaskFlow.Domain.Boards.BoardTask>();
    public DbSet<TaskFlow.Domain.Boards.TaskComment> TaskComments => Set<TaskFlow.Domain.Boards.TaskComment>();
    public DbSet<TaskFlow.Domain.Notifications.Notification> Notifications => Set<TaskFlow.Domain.Notifications.Notification>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (serviceProvider is not null && events.Count > 0)
        {
            var dispatcher = serviceProvider.GetService<IDomainEventDispatcher>();
            if (dispatcher is not null)
                await dispatcher.DispatchAsync(events, cancellationToken);
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity テーブル設定のため必ず先頭で呼ぶ

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
