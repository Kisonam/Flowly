using Flowly.Application.DTOs.Notes.Groups;

namespace Flowly.Application.Interfaces;

public interface INoteGroupService
{
    Task<List<NoteGroupDto>> GetAllAsync(Guid userId);
    Task<NoteGroupDto> GetByIdAsync(Guid userId, Guid id);
    Task<NoteGroupDto> CreateAsync(Guid userId, CreateNoteGroupDto dto);
    Task<NoteGroupDto> UpdateAsync(Guid userId, Guid id, UpdateNoteGroupDto dto);
    Task DeleteAsync(Guid userId, Guid id);
}
