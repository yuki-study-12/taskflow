using TaskFlow.Domain.Boards.Events;
using TaskFlow.Domain.Tasks.Events;

namespace TaskFlow.Domain.Boards;

public class BoardTask : Common.AggregateRoot<Guid>
{
    private readonly List<TaskComment> _comments = [];

    public Guid ColumnId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? AssigneeId { get; private set; }
    public Guid CreatorId { get; private set; }
    public int Order { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<TaskComment> Comments => _comments.AsReadOnly();

    private BoardTask(Guid id, Guid columnId, string title, string description, Guid? assigneeId, Guid creatorId, int order, DateTime createdAt)
        : base(id)
    {
        ColumnId = columnId;
        Title = title;
        Description = description;
        AssigneeId = assigneeId;
        CreatorId = creatorId;
        Order = order;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    // EF Core 用
    private BoardTask() { }

    public static BoardTask Create(Guid columnId, string title, string description, Guid? assigneeId, int order, Guid creatorId = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty.", nameof(title));

        return new BoardTask(Guid.NewGuid(), columnId, title, description ?? string.Empty, assigneeId, creatorId, order, DateTime.UtcNow);
    }

    public void Update(string title, string description, Guid? assigneeId)
    {
        var previousAssigneeId = AssigneeId;
        Title = title;
        Description = description ?? string.Empty;
        AssigneeId = assigneeId;
        UpdatedAt = DateTime.UtcNow;

        if (assigneeId.HasValue && assigneeId != previousAssigneeId)
            RaiseDomainEvent(new TaskAssignedEvent(Id, assigneeId.Value, DateTime.UtcNow));
    }

    public void Move(Guid newColumnId, int newOrder)
    {
        if (newColumnId == Guid.Empty)
            throw new ArgumentException("ColumnId cannot be empty.", nameof(newColumnId));
        if (newOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrder), "Order must be non-negative.");

        ColumnId = newColumnId;
        Order = newOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public TaskComment AddComment(Guid authorId, string body)
    {
        if (authorId == Guid.Empty)
            throw new ArgumentException("AuthorId cannot be empty.", nameof(authorId));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Comment body cannot be empty.", nameof(body));

        var comment = new TaskComment(Guid.NewGuid(), Id, authorId, body, DateTime.UtcNow);
        _comments.Add(comment);
        RaiseDomainEvent(new TaskCommentedEvent(Id, comment.Id, authorId, DateTime.UtcNow));
        return comment;
    }
}
