using TaskFlow.Domain.Boards;
using TaskFlow.Domain.Boards.Events;
using TaskFlow.Domain.Tasks.Events;
using Xunit;

namespace TaskFlow.Domain.Tests.Boards;

public class BoardTaskTests
{
    [Fact]
    public void Create_ValidInput_ReturnsTaskWithCorrectProperties()
    {
        var columnId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var task = BoardTask.Create(columnId, "タイトル", "説明", null, 0, creatorId);

        Assert.Equal(columnId, task.ColumnId);
        Assert.Equal("タイトル", task.Title);
        Assert.Equal(creatorId, task.CreatorId);
        Assert.Null(task.AssigneeId);
        Assert.Empty(task.DomainEvents);
    }

    [Fact]
    public void Update_AssigneeChanged_RaisesTaskAssignedEvent()
    {
        var task = BoardTask.Create(Guid.NewGuid(), "タイトル", "", null, 0, Guid.NewGuid());
        var assigneeId = Guid.NewGuid();

        task.Update("タイトル", "", assigneeId);

        var evt = Assert.Single(task.DomainEvents);
        var assigned = Assert.IsType<TaskAssignedEvent>(evt);
        Assert.Equal(task.Id, assigned.TaskId);
        Assert.Equal(assigneeId, assigned.AssigneeId);
    }

    [Fact]
    public void Update_AssigneeNotChanged_NoDomainEventRaised()
    {
        var assigneeId = Guid.NewGuid();
        var task = BoardTask.Create(Guid.NewGuid(), "タイトル", "", assigneeId, 0, Guid.NewGuid());
        task.ClearDomainEvents();

        task.Update("新しいタイトル", "", assigneeId);

        Assert.Empty(task.DomainEvents);
    }

    [Fact]
    public void Update_AssigneeSetToNull_NoDomainEventRaised()
    {
        var assigneeId = Guid.NewGuid();
        var task = BoardTask.Create(Guid.NewGuid(), "タイトル", "", assigneeId, 0, Guid.NewGuid());
        task.ClearDomainEvents();

        task.Update("タイトル", "", null);

        Assert.Empty(task.DomainEvents);
    }

    [Fact]
    public void AddComment_ValidInput_RaisesTaskCommentedEvent()
    {
        var task = BoardTask.Create(Guid.NewGuid(), "タイトル", "", Guid.NewGuid(), 0, Guid.NewGuid());
        task.ClearDomainEvents();

        var authorId = Guid.NewGuid();
        var comment = task.AddComment(authorId, "コメント本文");

        var evt = Assert.Single(task.DomainEvents);
        var commented = Assert.IsType<TaskCommentedEvent>(evt);
        Assert.Equal(task.Id, commented.TaskId);
        Assert.Equal(authorId, commented.AuthorId);
        Assert.Equal(comment.Id, commented.CommentId);
    }

    [Fact]
    public void AddComment_ValidInput_AddsToCommentsCollection()
    {
        var task = BoardTask.Create(Guid.NewGuid(), "タイトル", "", null, 0, Guid.NewGuid());
        var authorId = Guid.NewGuid();

        task.AddComment(authorId, "本文");

        var comment = Assert.Single(task.Comments);
        Assert.Equal(authorId, comment.AuthorId);
        Assert.Equal("本文", comment.Body);
    }

    [Fact]
    public void AddComment_EmptyAuthorId_ThrowsArgumentException()
    {
        var task = BoardTask.Create(Guid.NewGuid(), "タイトル", "", null, 0, Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => task.AddComment(Guid.Empty, "本文"));
    }

    [Fact]
    public void AddComment_EmptyBody_ThrowsArgumentException()
    {
        var task = BoardTask.Create(Guid.NewGuid(), "タイトル", "", null, 0, Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => task.AddComment(Guid.NewGuid(), "   "));
    }
}
