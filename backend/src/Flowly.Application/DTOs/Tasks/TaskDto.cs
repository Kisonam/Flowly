using Flowly.Domain.Enums;
using Flowly.Application.DTOs.Notes;

namespace Flowly.Application.DTOs.Tasks;

/// <summary>
/// Complete task information DTO
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TasksStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public string? Color { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    /// <summary>
    /// Позиція в межах теми / незакріплених.
    /// </summary>
    public int Order { get; set; }

    // Related entities
    public TaskThemeDto? Theme { get; set; }
    public List<SubtaskDto> Subtasks { get; set; } = new();
    public List<TagDto> Tags { get; set; } = new();
    public RecurrenceDto? Recurrence { get; set; }

    // Computed properties
    public bool IsOverdue { get; set; }
}
