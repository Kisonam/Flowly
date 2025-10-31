// backend/src/Flowly.Domain/Entities/TaskTag.cs
namespace Flowly.Domain.Entities;

public class TaskTag
{

    public Guid TaskId { get; set; }
    public Guid TagId { get; set; }

    // Navigation Properties
    public TaskItem Task { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}