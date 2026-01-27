using Flowly.Application.DTOs.Archive;
using Flowly.Domain.Enums;

namespace Flowly.Application.Interfaces;

public interface IArchiveService
{

    Task ArchiveEntityAsync(Guid userId, LinkEntityType entityType, Guid entityId);

    Task RestoreEntityAsync(Guid userId, Guid archiveEntryId);

    Task<ArchiveListResponseDto> GetArchivedAsync(Guid userId, ArchiveQueryDto query);

    Task<ArchivedEntityDetailDto> GetArchivedDetailAsync(Guid userId, Guid archiveEntryId);

    Task PermanentDeleteAsync(Guid userId, Guid archiveEntryId);
}
