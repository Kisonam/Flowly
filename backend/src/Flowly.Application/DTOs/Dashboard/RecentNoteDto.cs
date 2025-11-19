namespace Flowly.Application.DTOs.Dashboard;

/// <summary>
/// Lightweight note DTO for dashboard
/// </summary>
public class RecentNoteDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
