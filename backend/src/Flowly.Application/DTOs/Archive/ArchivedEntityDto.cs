using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Archive;

/// <summary>
/// DTO for archived entity in list view
/// </summary>
public class ArchivedEntityDto
{
    /// <summary>
    /// Archive entry ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of archived entity
    /// </summary>
    public LinkEntityType EntityType { get; set; }

    /// <summary>
    /// Original entity ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// When the entity was archived
    /// </summary>
    public DateTime ArchivedAt { get; set; }

    /// <summary>
    /// Display title (extracted from payload)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Display description (extracted from payload, optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Additional metadata from entity (e.g., amount, currency for transactions)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// DTO for archived entity detail view (includes full payload)
/// </summary>
public class ArchivedEntityDetailDto : ArchivedEntityDto
{
    /// <summary>
    /// Full JSON payload of the archived entity
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;
}
