using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Boards;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class ColumnConfiguration : IEntityTypeConfiguration<Column>
{
    public void Configure(EntityTypeBuilder<Column> builder)
    {
        builder.ToTable("Columns");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.BoardId).IsRequired();
        builder.Property(c => c.Order).IsRequired();

        builder.HasMany(c => c.Tasks)
            .WithOne()
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
