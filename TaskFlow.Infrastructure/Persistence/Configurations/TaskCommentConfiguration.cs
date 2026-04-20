using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TaskId).IsRequired();
        builder.Property(c => c.AuthorId).IsRequired();

        builder.Property(c => c.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.TaskId);
    }
}
