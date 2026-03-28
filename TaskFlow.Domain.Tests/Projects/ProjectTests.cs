using TaskFlow.Domain.Projects;
using TaskFlow.Domain.Projects.Events;
using Xunit;

namespace TaskFlow.Domain.Tests.Projects;

public class ProjectTests
{
    [Fact]
    public void Create_ValidInput_ReturnsProjectWithCorrectProperties()
    {
        var ownerId = Guid.NewGuid();

        var project = Project.Create("My Project", "A description", ownerId);

        Assert.Equal("My Project", project.Name);
        Assert.Equal("A description", project.Description);
    }

    [Fact]
    public void Create_ValidInput_OwnerIsAddedAsMember()
    {
        var ownerId = Guid.NewGuid();

        var project = Project.Create("My Project", "A description", ownerId);

        Assert.Single(project.Members);
        Assert.Equal(ownerId, project.Members[0].UserId);
        Assert.Equal(MemberRole.Owner, project.Members[0].Role);
    }

    [Fact]
    public void Create_ValidInput_RaisesProjectCreatedEvent()
    {
        var ownerId = Guid.NewGuid();

        var project = Project.Create("My Project", "A description", ownerId);

        var evt = Assert.Single(project.DomainEvents);
        var createdEvent = Assert.IsType<ProjectCreatedEvent>(evt);
        Assert.Equal(project.Id, createdEvent.ProjectId);
        Assert.Equal(ownerId, createdEvent.OwnerId);
    }

    [Fact]
    public void AddMember_NewUser_AddsMemberToList()
    {
        var project = Project.Create("My Project", "desc", Guid.NewGuid());
        project.ClearDomainEvents();
        var userId = Guid.NewGuid();

        project.AddMember(userId, MemberRole.Member);

        Assert.Equal(2, project.Members.Count);
        Assert.Contains(project.Members, m => m.UserId == userId && m.Role == MemberRole.Member);
    }

    [Fact]
    public void AddMember_NewUser_RaisesMemberInvitedEvent()
    {
        var project = Project.Create("My Project", "desc", Guid.NewGuid());
        project.ClearDomainEvents();
        var userId = Guid.NewGuid();

        project.AddMember(userId, MemberRole.Admin);

        var evt = Assert.Single(project.DomainEvents);
        var invitedEvent = Assert.IsType<MemberInvitedEvent>(evt);
        Assert.Equal(project.Id, invitedEvent.ProjectId);
        Assert.Equal(userId, invitedEvent.UserId);
        Assert.Equal(MemberRole.Admin, invitedEvent.Role);
    }

    [Theory]
    [InlineData("", "desc")]
    [InlineData("   ", "desc")]
    [InlineData("name", "")]
    [InlineData("name", "   ")]
    public void Create_InvalidInput_ThrowsArgumentException(string name, string description)
    {
        Assert.Throws<ArgumentException>(() => Project.Create(name, description, Guid.NewGuid()));
    }

    [Fact]
    public void AddMember_OwnerRole_ThrowsInvalidOperationException()
    {
        var project = Project.Create("My Project", "desc", Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => project.AddMember(Guid.NewGuid(), MemberRole.Owner));
    }

    [Fact]
    public void AddMember_DuplicateUser_ThrowsInvalidOperationException()
    {
        var project = Project.Create("My Project", "desc", Guid.NewGuid());
        var userId = Guid.NewGuid();
        project.AddMember(userId, MemberRole.Member);

        Assert.Throws<InvalidOperationException>(() => project.AddMember(userId, MemberRole.Admin));
    }

    [Fact]
    public void RemoveMember_ExistingNonOwnerUser_RemovesMemberFromList()
    {
        var project = Project.Create("My Project", "desc", Guid.NewGuid());
        var userId = Guid.NewGuid();
        project.AddMember(userId, MemberRole.Member);

        project.RemoveMember(userId);

        Assert.DoesNotContain(project.Members, m => m.UserId == userId);
    }

    [Fact]
    public void RemoveMember_Owner_ThrowsInvalidOperationException()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("My Project", "desc", ownerId);

        Assert.Throws<InvalidOperationException>(() => project.RemoveMember(ownerId));
    }

    [Fact]
    public void RemoveMember_NonExistentUser_DoesNotThrow()
    {
        var project = Project.Create("My Project", "desc", Guid.NewGuid());

        var exception = Record.Exception(() => project.RemoveMember(Guid.NewGuid()));

        Assert.Null(exception);
    }
}
