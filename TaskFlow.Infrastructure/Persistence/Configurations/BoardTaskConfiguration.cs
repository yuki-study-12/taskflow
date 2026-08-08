using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class BoardTaskConfiguration : IEntityTypeConfiguration<BoardTask>
{
    public void Configure(EntityTypeBuilder<BoardTask> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.ColumnId).IsRequired();
        builder.Property(t => t.CreatorId).IsRequired();
        builder.Property(t => t.Order).IsRequired();
        builder.Property(t => t.DueDate);
        builder.Property(t => t.Priority)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.Ignore(t => t.DomainEvents);

        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
