namespace Flowly.Domain.Entities;

public class TaskSubtask
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; } = false;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public TaskItem TaskItem { get; set; } = null!;

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Subtask title cannot be empty", nameof(title));
        Title = title.Trim();
    }

    public void Toggle()
    {
        IsDone = !IsDone;
        CompletedAt = IsDone ? DateTime.UtcNow : null;
    }

    public void MarkAsDone()
    {
        IsDone = true;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsNotDone()
    {
        IsDone = false;
        CompletedAt = null;
    }
}