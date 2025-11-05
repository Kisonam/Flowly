namespace Flowly.Application.DTOs.Notes.Groups;

public class NoteGroupDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Color { get; set; }
}
