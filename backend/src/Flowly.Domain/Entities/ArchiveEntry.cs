using Flowly.Domain.Enums;

namespace Flowly.Domain.Entities;

public class ArchiveEntry
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public LinkEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    public string GetDescription()
    {
        return $"{EntityType} archived at {ArchivedAt:yyyy-MM-dd HH:mm}";
    }
}