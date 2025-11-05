using Flowly.Application.DTOs.Tasks;

namespace Flowly.Application.Interfaces;

public interface ITaskThemeService
{
    Task<List<TaskThemeDto>> GetAllAsync(Guid userId);
    Task<TaskThemeDto> GetByIdAsync(Guid userId, Guid id);
    Task<TaskThemeDto> CreateAsync(Guid userId, CreateTaskThemeDto dto);
    Task<TaskThemeDto> UpdateAsync(Guid userId, Guid id, UpdateTaskThemeDto dto);
    Task DeleteAsync(Guid userId, Guid id);
}
