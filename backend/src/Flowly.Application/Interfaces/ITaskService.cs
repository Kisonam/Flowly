using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Tasks;

namespace Flowly.Application.Interfaces;

public interface ITaskService
{
    
    Task<PagedResult<TaskDto>> GetAllTasksAsync(Guid userId, TaskFilterDto filter);
    Task<TaskDto> GetTaskByIdAsync(Guid userId, Guid taskId);
    Task<TaskDto> CreateTaskAsync(Guid userId, CreateTaskDto dto);
    Task<TaskDto> UpdateTaskAsync(Guid userId, Guid taskId, UpdateTaskDto dto);

    Task ArchiveTaskAsync(Guid userId, Guid taskId);
    Task RestoreTaskAsync(Guid userId, Guid taskId);

    Task<List<TaskThemeDto>> GetThemesAsync(Guid userId);
    Task<TaskThemeDto> CreateThemeAsync(Guid userId, CreateTaskThemeDto dto);
    Task<TaskThemeDto> UpdateThemeAsync(Guid userId, Guid themeId, UpdateTaskThemeDto dto);
    Task DeleteThemeAsync(Guid userId, Guid themeId);
    Task ReorderThemesAsync(Guid userId, List<Guid> themeIds);
    Task MoveTaskToThemeAsync(Guid userId, Guid taskId, Guid? newThemeId);

    Task<SubtaskDto> AddSubtaskAsync(Guid userId, Guid taskId, CreateSubtaskDto dto);
    Task<SubtaskDto> UpdateSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId, UpdateSubtaskDto dto);
    Task DeleteSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId);
    Task<SubtaskDto> ToggleSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId);

    Task<RecurrenceDto> SetRecurrenceAsync(Guid userId, Guid taskId, CreateRecurrenceDto dto);
    Task RemoveRecurrenceAsync(Guid userId, Guid taskId);

    Task AddTagAsync(Guid userId, Guid taskId, Guid tagId);
    Task RemoveTagAsync(Guid userId, Guid taskId, Guid tagId);

    Task ReorderTasksAsync(Guid userId, List<(Guid TaskId, Guid? ThemeId, int Order)> items);
    Task CompleteTaskAsync(Guid userId, Guid taskId);
    Task ChangeStatusAsync(Guid userId, Guid taskId, Flowly.Domain.Enums.TasksStatus status);
}
