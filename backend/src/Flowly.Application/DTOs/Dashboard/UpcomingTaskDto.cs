using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Dashboard;

public class UpcomingTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public TasksStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public string? Color { get; set; }
    public bool IsOverdue { get; set; }
}
