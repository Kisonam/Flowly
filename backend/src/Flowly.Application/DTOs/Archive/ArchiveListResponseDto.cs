namespace Flowly.Application.DTOs.Archive;

/// <summary>
/// Paginated response for archived entities
/// </summary>
public class ArchiveListResponseDto
{
    /// <summary>
    /// List of archived entities
    /// </summary>
    public List<ArchivedEntityDto> Items { get; set; } = new();

    /// <summary>
    /// Total count of items matching the query
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
