namespace Flowly.Application.DTOs.Notes;

public class CreateNoteDto
{
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public List<Guid>? TagIds { get; set; }
    public Guid? GroupId { get; set; }
}
