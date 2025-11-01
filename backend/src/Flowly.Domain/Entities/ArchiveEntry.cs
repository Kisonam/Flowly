// backend/src/Flowly.Domain/Entities/ArchiveEntry.cs

using Flowly.Domain.Enums;

namespace Flowly.Domain.Entities;

/// <summary>
/// Archive entry - stores snapshot of archived entity for potential restoration
/// </summary>
public class ArchiveEntry
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of the archived item
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of archived entity
    /// </summary>
    public LinkEntityType EntityType { get; set; }

    /// <summary>
    /// ID of the archived entity
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// JSON snapshot of the entity at archive time
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// When the entity was archived
    /// </summary>
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    // ============================================
    // Methods
    // ============================================

    /// <summary>
    /// Get a description of the archived item
    /// </summary>
    public string GetDescription()
    {
        return $"{EntityType} archived at {ArchivedAt:yyyy-MM-dd HH:mm}";
    }
}