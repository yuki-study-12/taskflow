namespace TaskFlow.Web.Models;

public sealed class KanbanTaskState
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
}

public sealed class KanbanColumnState
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int Order { get; set; }
    public List<KanbanTaskState> Tasks { get; init; } = [];
}
