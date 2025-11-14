using Flowly.Application.DTOs.Archive;
using Flowly.Domain.Enums;

namespace Flowly.Application.Interfaces;

/// <summary>
/// Service for managing archived entities
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Archive an entity (soft delete + create JSON snapshot)
    /// </summary>
    /// <param name="userId">User ID who owns the entity</param>
    /// <param name="entityType">Type of entity to archive</param>
    /// <param name="entityId">ID of the entity to archive</param>
    Task ArchiveEntityAsync(Guid userId, LinkEntityType entityType, Guid entityId);

    /// <summary>
    /// Restore an archived entity from JSON snapshot
    /// </summary>
    /// <param name="userId">User ID who owns the archive entry</param>
    /// <param name="archiveEntryId">ID of the archive entry</param>
    Task RestoreEntityAsync(Guid userId, Guid archiveEntryId);

    /// <summary>
    /// Get list of archived entities with pagination
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="query">Query parameters</param>
    /// <returns>Paginated list of archived entities</returns>
    Task<ArchiveListResponseDto> GetArchivedAsync(Guid userId, ArchiveQueryDto query);

    /// <summary>
    /// Get detailed information about a specific archived entity (includes full payload)
    /// </summary>
    /// <param name="userId">User ID who owns the archive entry</param>
    /// <param name="archiveEntryId">ID of the archive entry</param>
    /// <returns>Detailed archived entity with full JSON payload</returns>
    Task<ArchivedEntityDetailDto> GetArchivedDetailAsync(Guid userId, Guid archiveEntryId);

    /// <summary>
    /// Permanently delete an archived entity
    /// </summary>
    /// <param name="userId">User ID who owns the archive entry</param>
    /// <param name="archiveEntryId">ID of the archive entry to permanently delete</param>
    Task PermanentDeleteAsync(Guid userId, Guid archiveEntryId);
}
