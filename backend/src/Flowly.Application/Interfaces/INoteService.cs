using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Notes;

namespace Flowly.Application.Interfaces;

public interface INoteService
{
    /// <summary>
    /// Get all notes for a user with optional filtering and pagination
    /// </summary>
    Task<PagedResult<NoteDto>> GetAllAsync(Guid userId, NoteFilterDto filter);

    /// <summary>
    /// Get a specific note by ID
    /// </summary>
    Task<NoteDto> GetByIdAsync(Guid userId, Guid noteId);

    /// <summary>
    /// Create a new note
    /// </summary>
    Task<NoteDto> CreateAsync(Guid userId, CreateNoteDto dto);

    /// <summary>
    /// Update an existing note
    /// </summary>
    Task<NoteDto> UpdateAsync(Guid userId, Guid noteId, UpdateNoteDto dto);

    /// <summary>
    /// Archive a note (soft delete)
    /// </summary>
    Task ArchiveAsync(Guid userId, Guid noteId);

    /// <summary>
    /// Restore an archived note
    /// </summary>
    Task RestoreAsync(Guid userId, Guid noteId);

    /// <summary>
    /// Add a tag to a note
    /// </summary>
    Task AddTagAsync(Guid userId, Guid noteId, Guid tagId);

    /// <summary>
    /// Remove a tag from a note
    /// </summary>
    Task RemoveTagAsync(Guid userId, Guid noteId, Guid tagId);

    /// <summary>
    /// Upload media asset to a note
    /// </summary>
    Task<string> UploadMediaAsync(Guid userId, Guid noteId, Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Export note as markdown file
    /// </summary>
    Task<string> ExportMarkdownAsync(Guid userId, Guid noteId);
}
