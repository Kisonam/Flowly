namespace Flowly.Application.DTOs.Notes;

public class NoteDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string? HtmlCache { get; set; }
    public bool IsArchived { get; set; }
    public List<TagDto> Tags { get; set; } = new();
    public Guid? GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
