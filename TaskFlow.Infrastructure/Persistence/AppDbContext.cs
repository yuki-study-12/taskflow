using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{

    public DbSet<TaskFlow.Domain.Projects.Project> Projects => Set<TaskFlow.Domain.Projects.Project>();
    public DbSet<TaskFlow.Domain.Boards.Board> Boards => Set<TaskFlow.Domain.Boards.Board>();
    public DbSet<TaskFlow.Domain.Boards.Column> Columns => Set<TaskFlow.Domain.Boards.Column>();
    public DbSet<TaskFlow.Domain.Boards.BoardTask> Tasks => Set<TaskFlow.Domain.Boards.BoardTask>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity テーブル設定のため必ず先頭で呼ぶ

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
