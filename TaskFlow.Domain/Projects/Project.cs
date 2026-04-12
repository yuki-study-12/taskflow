namespace TaskFlow.Domain.Projects;

public class Project : Common.AggregateRoot<Guid>
{
    private readonly List<ProjectMember> _members = [];
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Project (Guid id, string name, string description) : base(id)
    {
        Name = name;
        Description = description;
    }

    // EF Core 用
    private Project() { }

    public IReadOnlyList<ProjectMember> Members => _members.AsReadOnly();

    public static Project Create (string name, string description, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Project description cannot be empty.", nameof(description));

        var project = new Project(Guid.NewGuid(), name, description);
        project._members.Add(new ProjectMember(Guid.NewGuid(), ownerId, MemberRole.Owner));
        project.RaiseDomainEvent(new Events.ProjectCreatedEvent(project.Id, ownerId, DateTime.UtcNow));
        return project;
    }

    public void AddMember (Guid userId, MemberRole role)
    {
        if (role == MemberRole.Owner)
            throw new InvalidOperationException("Owner role cannot be assigned via AddMember.");

        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException($"User {userId} is already a member of this project.");

        var member = new ProjectMember(Guid.NewGuid(), userId, role);
        _members.Add(member);
        RaiseDomainEvent(new Events.MemberInvitedEvent(Id, userId, role, DateTime.UtcNow));
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void UpdateMemberRole(Guid userId, MemberRole newRole)
    {
        if (newRole == MemberRole.Owner)
            throw new InvalidOperationException("Owner role cannot be assigned via UpdateMemberRole.");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException($"User {userId} is not a member of this project.");

        if (member.Role == MemberRole.Owner)
            throw new InvalidOperationException("The project owner's role cannot be changed.");

        member.UpdateRole(newRole);
    }

    public void RemoveMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId && m.Role == MemberRole.Owner))
        {
            throw new InvalidOperationException("The project owner cannot be removed.");
        }

        _members.RemoveAll(m => m.UserId == userId);
    }
}
