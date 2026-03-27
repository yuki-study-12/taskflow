using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{

    // 将来のドメイン DbSet はここに追加
    // public DbSet<Project> Projects => Set<Project>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity テーブル設定のため必ず先頭で呼ぶ

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
