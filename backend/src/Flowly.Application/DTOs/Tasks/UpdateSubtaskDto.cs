namespace Flowly.Application.DTOs.Tasks;

/// <summary>
/// DTO for updating a subtask
/// </summary>
public class UpdateSubtaskDto
{
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}
