using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Tasks;

/// <summary>
/// DTO for filtering and paginating tasks
/// </summary>
public class TaskFilterDto
{
    public string? Search { get; set; }
    public List<Guid>? TagIds { get; set; }
    public List<Guid>? ThemeIds { get; set; }
    public TasksStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public bool? IsArchived { get; set; }
    public bool? IsOverdue { get; set; }
    /// <summary>
    /// Match tasks whose DueDate falls on this exact calendar day (UTC) ignoring time component.
    /// </summary>
    public DateTime? DueDateOn { get; set; }
    public DateTime? DueDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
