using Flowly.Domain.Enums;

namespace Flowly.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TaskThemeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Color { get; set; }
    public TasksStatus Status { get; set; } = TasksStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.None;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public TaskTheme? TaskTheme { get; set; }
    public ICollection<TaskSubtask> Subtasks { get; set; } = new List<TaskSubtask>();
    public TaskRecurrence? Recurrence { get; set; }
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    public ICollection<Link> LinksFrom { get; set; } = new List<Link>();
    public ICollection<Link> LinksTo { get; set; } = new List<Link>();

    // Methods
    public void UpdateContent(string title, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty", nameof(title));
        Title = title.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TasksStatus newStatus)
    {
        Status = newStatus;
        if (newStatus == TasksStatus.Done && CompletedAt == null)
            CompletedAt = DateTime.UtcNow;
        else if (newStatus != TasksStatus.Done)
            CompletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete() => ChangeStatus(TasksStatus.Done);
    public void Reopen() => ChangeStatus(TasksStatus.Todo);

    public void ChangePriority(TaskPriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToTheme(Guid? themeId)
    {
        TaskThemeId = themeId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue()
    {
        return DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status != TasksStatus.Done;
    }
}
