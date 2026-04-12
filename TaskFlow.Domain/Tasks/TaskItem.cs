namespace TaskFlow.Domain.Tasks;

public class TaskItem : Common.AggregateRoot<Guid>
{
    private readonly List<Comment> _comments = [];

    public string Title { get; private set; } = string.Empty;
    public Guid ColumnId { get; private set; }
    public int Order { get; private set; }
    public Guid CreatorId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Priority Priority { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<Comment> Comments => _comments.AsReadOnly();

    private TaskItem(Guid id, string title, Guid columnId, Guid creatorId, DateTime createdAt) : base(id)
    {
        Title = title;
        ColumnId = columnId;
        CreatorId = creatorId;
        Priority = Priority.Medium;
        Status = TaskItemStatus.Todo;
        CreatedAt = createdAt;
    }

    // EF Core 用
    private TaskItem() { }

    public static TaskItem Create(string title, Guid columnId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty.", nameof(title));
        if (columnId == Guid.Empty)
            throw new ArgumentException("ColumnId cannot be empty.", nameof(columnId));
        if (creatorId == Guid.Empty)
            throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));

        var task = new TaskItem(Guid.NewGuid(), title, columnId, creatorId, DateTime.UtcNow);
        task.RaiseDomainEvent(new Events.TaskCreatedEvent(task.Id, title, columnId, creatorId, DateTime.UtcNow));
        return task;
    }

    public void Assign(Guid assigneeId)
    {
        if (assigneeId == Guid.Empty)
            throw new ArgumentException("AssigneeId cannot be empty.", nameof(assigneeId));

        AssigneeId = assigneeId;
        RaiseDomainEvent(new Events.TaskAssignedEvent(Id, assigneeId, DateTime.UtcNow));
    }

    public void Move(Guid newColumnId, int newOrder)
    {
        if (newColumnId == Guid.Empty)
            throw new ArgumentException("ColumnId cannot be empty.", nameof(newColumnId));
        if (newOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrder), "Order must be non-negative.");

        ColumnId = newColumnId;
        Order = newOrder;
        RaiseDomainEvent(new Events.TaskMovedEvent(Id, newColumnId, newOrder, DateTime.UtcNow));
    }

    public void AddComment(Guid authorId, string body)
    {
        if (authorId == Guid.Empty)
            throw new ArgumentException("AuthorId cannot be empty.", nameof(authorId));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Comment body cannot be empty.", nameof(body));

        var comment = new Comment(Guid.NewGuid(), authorId, body, DateTime.UtcNow);
        _comments.Add(comment);
    }
}
