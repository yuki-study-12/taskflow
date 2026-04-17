namespace TaskFlow.Domain.Boards;

public class Column : Common.Entity<Guid>
{
    private readonly List<BoardTask> _tasks = [];

    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public IReadOnlyList<BoardTask> Tasks => _tasks.AsReadOnly();

    private Column(Guid id, Guid boardId, string name, int order) : base(id)
    {
        BoardId = boardId;
        Name = name;
        Order = order;
    }

    // EF Core 用
    private Column() { }

    public static Column Create(Guid boardId, string name, int order)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be empty.", nameof(name));

        return new Column(Guid.NewGuid(), boardId, name, order);
    }

    public void Update(string name)
    {
        Name = name;
    }
}
