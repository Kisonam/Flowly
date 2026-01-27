namespace Flowly.Application.DTOs.Tasks;

public class UpdateSubtaskDto
{
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}
