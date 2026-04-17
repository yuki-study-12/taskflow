namespace TaskFlow.Domain.Tasks;

public class Comment : Common.Entity<Guid>
{
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    internal Comment(Guid id, Guid authorId, string body, DateTime createdAt) : base(id)
    {
        AuthorId = authorId;
        Body = body;
        CreatedAt = createdAt;
    }

    // EF Core 用
    private Comment() { }
}
