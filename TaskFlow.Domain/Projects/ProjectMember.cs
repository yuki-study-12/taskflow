namespace TaskFlow.Domain.Projects;

public class ProjectMember : Common.Entity<Guid>
{
    public Guid UserId {get; private set;}
    public MemberRole Role {get; private set;}

    public ProjectMember(Guid id, Guid userId, MemberRole role) : base(id)
    {
        UserId = userId;
        Role = role;
    }

    public void UpdateRole(MemberRole newRole)
    {
        Role = newRole;
    }

    // EF Core 用
    private ProjectMember() { Role = MemberRole.Member; }
}
