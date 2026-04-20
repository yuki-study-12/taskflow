namespace TaskFlow.Domain.Boards;

public class TaskComment : Common.Entity<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    internal TaskComment(Guid id, Guid taskId, Guid authorId, string body, DateTime createdAt) : base(id)
    {
        TaskId = taskId;
        AuthorId = authorId;
        Body = body;
        CreatedAt = createdAt;
    }

    // EF Core 用
    private TaskComment() { }
}
