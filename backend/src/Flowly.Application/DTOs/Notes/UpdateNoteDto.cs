namespace Flowly.Application.DTOs.Notes;

public class UpdateNoteDto
{
    public string? Title { get; set; }
    public string? Markdown { get; set; }
    public List<Guid>? TagIds { get; set; }
    public Guid? GroupId { get; set; }
}
