using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Archive;

public class ArchiveQueryDto
{

    public LinkEntityType? EntityType { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string SortBy { get; set; } = "ArchivedAt";

    public string SortDirection { get; set; } = "desc";
}
