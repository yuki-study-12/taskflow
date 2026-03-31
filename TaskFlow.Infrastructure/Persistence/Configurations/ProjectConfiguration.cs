using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Projects;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.OwnsMany(p => p.Members, m =>
        {
            m.ToTable("ProjectMembers");
            m.WithOwner().HasForeignKey("ProjectId");
            m.HasKey(pm => pm.Id);
            m.Property(pm => pm.Id).ValueGeneratedNever();

            m.Property(pm => pm.UserId).IsRequired();

            m.Property(pm => pm.Role)
                .HasConversion(
                    role => role.Value,
                    value => MemberRole.FromValue(value)
                )
                .HasMaxLength(50)
                .IsRequired();
        });
    }
}
