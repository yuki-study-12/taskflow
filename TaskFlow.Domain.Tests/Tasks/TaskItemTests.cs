using TaskFlow.Domain.Tasks;
using TaskFlow.Domain.Tasks.Events;
using Xunit;

namespace TaskFlow.Domain.Tests.Tasks;

public class TaskItemTests
{
    [Fact]
    public void Create_ValidInput_ReturnsTaskWithCorrectProperties()
    {
        var columnId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var task = TaskItem.Create("My Task", columnId, creatorId);

        Assert.Equal("My Task", task.Title);
        Assert.Equal(columnId, task.ColumnId);
        Assert.Equal(creatorId, task.CreatorId);
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Equal(Priority.Medium, task.Priority);
        Assert.Null(task.AssigneeId);
    }

    [Fact]
    public void Create_ValidInput_RaisesTaskCreatedEvent()
    {
        var columnId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var task = TaskItem.Create("My Task", columnId, creatorId);

        var evt = Assert.Single(task.DomainEvents);
        var createdEvent = Assert.IsType<TaskCreatedEvent>(evt);
        Assert.Equal(task.Id, createdEvent.TaskId);
        Assert.Equal("My Task", createdEvent.Title);
        Assert.Equal(columnId, createdEvent.ColumnId);
        Assert.Equal(creatorId, createdEvent.CreatorId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_ThrowsArgumentException(string title)
    {
        Assert.Throws<ArgumentException>(() =>
            TaskItem.Create(title, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Assign_ValidAssignee_SetsAssigneeId()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());
        task.ClearDomainEvents();
        var assigneeId = Guid.NewGuid();

        task.Assign(assigneeId);

        Assert.Equal(assigneeId, task.AssigneeId);
    }

    [Fact]
    public void Assign_ValidAssignee_RaisesTaskAssignedEvent()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());
        task.ClearDomainEvents();
        var assigneeId = Guid.NewGuid();

        task.Assign(assigneeId);

        var evt = Assert.Single(task.DomainEvents);
        var assignedEvent = Assert.IsType<TaskAssignedEvent>(evt);
        Assert.Equal(task.Id, assignedEvent.TaskId);
        Assert.Equal(assigneeId, assignedEvent.AssigneeId);
    }

    [Fact]
    public void Assign_EmptyAssigneeId_ThrowsArgumentException()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => task.Assign(Guid.Empty));
    }

    [Fact]
    public void Move_ValidInput_UpdatesColumnIdAndOrder()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());
        task.ClearDomainEvents();
        var newColumnId = Guid.NewGuid();

        task.Move(newColumnId, 3);

        Assert.Equal(newColumnId, task.ColumnId);
        Assert.Equal(3, task.Order);
    }

    [Fact]
    public void Move_ValidInput_RaisesTaskMovedEvent()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());
        task.ClearDomainEvents();
        var newColumnId = Guid.NewGuid();

        task.Move(newColumnId, 2);

        var evt = Assert.Single(task.DomainEvents);
        var movedEvent = Assert.IsType<TaskMovedEvent>(evt);
        Assert.Equal(task.Id, movedEvent.TaskId);
        Assert.Equal(newColumnId, movedEvent.NewColumnId);
        Assert.Equal(2, movedEvent.NewOrder);
    }

    [Fact]
    public void Move_NegativeOrder_ThrowsArgumentOutOfRangeException()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => task.Move(Guid.NewGuid(), -1));
    }

    [Fact]
    public void Move_EmptyColumnId_ThrowsArgumentException()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => task.Move(Guid.Empty, 0));
    }

    [Fact]
    public void AddComment_ValidInput_AddsCommentToList()
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());
        var authorId = Guid.NewGuid();

        task.AddComment(authorId, "This is a comment.");

        Assert.Single(task.Comments);
        Assert.Equal(authorId, task.Comments[0].AuthorId);
        Assert.Equal("This is a comment.", task.Comments[0].Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddComment_EmptyBody_ThrowsArgumentException(string body)
    {
        var task = TaskItem.Create("My Task", Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => task.AddComment(Guid.NewGuid(), body));
    }
}
