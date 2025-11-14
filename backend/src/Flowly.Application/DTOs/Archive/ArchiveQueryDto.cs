using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Archive;

/// <summary>
/// Query parameters for retrieving archived entities
/// </summary>
public class ArchiveQueryDto
{
    /// <summary>
    /// Filter by entity type (optional)
    /// </summary>
    public LinkEntityType? EntityType { get; set; }

    /// <summary>
    /// Search query (searches in title/description)
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort by field (ArchivedAt, Title, EntityType)
    /// </summary>
    public string SortBy { get; set; } = "ArchivedAt";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}
