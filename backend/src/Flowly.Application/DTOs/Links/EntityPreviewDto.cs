using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Links;

public class EntityPreviewDto
{
    public LinkEntityType Type { get; set; }
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public string? IconUrl { get; set; }
}
