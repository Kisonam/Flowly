using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Links;

public class LinkDto
{
    public Guid Id { get; set; }
    public LinkEntityType FromType { get; set; }
    public Guid FromId { get; set; }
    public LinkEntityType ToType { get; set; }
    public Guid ToId { get; set; }
    public EntityPreviewDto? FromPreview { get; set; }
    public EntityPreviewDto? ToPreview { get; set; }
    public DateTime CreatedAt { get; set; }
}
