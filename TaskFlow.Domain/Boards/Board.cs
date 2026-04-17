namespace TaskFlow.Domain.Boards;

public class Board : Common.AggregateRoot<Guid>
{
    private readonly List<Column> _columns = [];

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyList<Column> Columns => _columns.AsReadOnly();

    private Board(Guid id, Guid projectId, string name) : base(id)
    {
        ProjectId = projectId;
        Name = name;
    }

    // EF Core 用
    private Board() { }

    public static Board Create(Guid projectId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board name cannot be empty.", nameof(name));

        return new Board(Guid.NewGuid(), projectId, name);
    }
}
