using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Links;

public class CreateLinkDto
{
    public LinkEntityType FromType { get; set; }
    public Guid FromId { get; set; }
    public LinkEntityType ToType { get; set; }
    public Guid ToId { get; set; }
}
