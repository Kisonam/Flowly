
namespace Flowly.Domain.Entities;

public class TaskRecurrence
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public string Rule { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastOccurrence { get; set; }
    public DateTime? NextOccurrence { get; set; }

    // Navigation Properties
    public TaskItem TaskItem { get; set; } = null!;

    // Methods
    public void UpdateRule(string rule)
    {
        if (string.IsNullOrWhiteSpace(rule))
            throw new ArgumentException("Recurrence rule cannot be empty", nameof(rule));
        Rule = rule;
    }
}