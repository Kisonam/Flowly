using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Tasks;

/// <summary>
/// DTO for creating a new task
/// </summary>
public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? ThemeId { get; set; }
    public string? Color { get; set; }
    public TaskPriority? Priority { get; set; }
    public List<Guid>? TagIds { get; set; }
}
