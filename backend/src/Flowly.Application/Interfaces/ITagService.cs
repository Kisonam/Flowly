using Flowly.Application.DTOs.Notes;
using Flowly.Application.DTOs.Tags;

namespace Flowly.Application.Interfaces;

public interface ITagService
{
    Task<List<TagDto>> GetAllAsync(Guid userId);
    Task<TagDto> GetByIdAsync(Guid userId, Guid tagId);
    Task<TagDto> CreateAsync(Guid userId, CreateTagDto dto);
    Task<TagDto> UpdateAsync(Guid userId, Guid tagId, UpdateTagDto dto);
    Task DeleteAsync(Guid userId, Guid tagId);
}
