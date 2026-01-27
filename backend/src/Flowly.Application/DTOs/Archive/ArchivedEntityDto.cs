using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Archive;

public class ArchivedEntityDto
{

    public Guid Id { get; set; }

    public LinkEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public DateTime ArchivedAt { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}

public class ArchivedEntityDetailDto : ArchivedEntityDto
{

    public string PayloadJson { get; set; } = string.Empty;
}
