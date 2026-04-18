namespace TaskFlow.Domain.Boards;

public class BoardTask : Common.Entity<Guid>
{
    public Guid ColumnId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? AssigneeId { get; private set; }
    public int Order { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private BoardTask(Guid id, Guid columnId, string title, string description, Guid? assigneeId, int order, DateTime createdAt)
        : base(id)
    {
        ColumnId = columnId;
        Title = title;
        Description = description;
        AssigneeId = assigneeId;
        Order = order;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    // EF Core 用
    private BoardTask() { }

    public static BoardTask Create(Guid columnId, string title, string description, Guid? assigneeId, int order)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty.", nameof(title));

        return new BoardTask(Guid.NewGuid(), columnId, title, description ?? string.Empty, assigneeId, order, DateTime.UtcNow);
    }

    public void Update(string title, string description, Guid? assigneeId)
    {
        Title = title;
        Description = description ?? string.Empty;
        AssigneeId = assigneeId;
        UpdatedAt = DateTime.UtcNow;
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
}
