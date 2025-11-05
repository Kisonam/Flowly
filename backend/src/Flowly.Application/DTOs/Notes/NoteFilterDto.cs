namespace Flowly.Application.DTOs.Notes;

public class NoteFilterDto
{
    public string? Search { get; set; }
    public List<Guid>? TagIds { get; set; }
    public bool? IsArchived { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
