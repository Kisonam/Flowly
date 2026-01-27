using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Notes;

namespace Flowly.Application.Interfaces;

public interface INoteService
{

    Task<PagedResult<NoteDto>> GetAllAsync(Guid userId, NoteFilterDto filter);

    Task<NoteDto> GetByIdAsync(Guid userId, Guid noteId);

    Task<NoteDto> CreateAsync(Guid userId, CreateNoteDto dto);

    Task<NoteDto> UpdateAsync(Guid userId, Guid noteId, UpdateNoteDto dto);

    Task ArchiveAsync(Guid userId, Guid noteId);

    Task RestoreAsync(Guid userId, Guid noteId);

    Task AddTagAsync(Guid userId, Guid noteId, Guid tagId);

    Task RemoveTagAsync(Guid userId, Guid noteId, Guid tagId);

    Task<string> UploadMediaAsync(Guid userId, Guid noteId, Stream fileStream, string fileName, string contentType);

    Task<string> ExportMarkdownAsync(Guid userId, Guid noteId);
}
