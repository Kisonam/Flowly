using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Tasks;

/// <summary>
/// DTO for updating an existing task
/// </summary>
public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TasksStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public string? Color { get; set; }
    public Guid? ThemeId { get; set; }
    public List<Guid>? TagIds { get; set; }
}
