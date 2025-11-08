using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Tasks;

namespace Flowly.Application.Interfaces;

/// <summary>
/// Service for managing tasks, subtasks, recurrence, and related operations
/// </summary>
public interface ITaskService
{
    // ============================================
    // Task CRUD Operations
    // ============================================

    /// <summary>
    /// Get all tasks for a user with optional filtering and pagination
    /// Supports filtering by themes, status, priority, tags, due date, etc.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="filter">Filter criteria including pagination</param>
    /// <returns>Paginated list of tasks</returns>
    Task<PagedResult<TaskDto>> GetAllTasksAsync(Guid userId, TaskFilterDto filter);

    /// <summary>
    /// Get a specific task by ID with all related data (subtasks, tags, theme, recurrence)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    /// <returns>Complete task information</returns>
    Task<TaskDto> GetTaskByIdAsync(Guid userId, Guid taskId);

    /// <summary>
    /// Create a new task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Task creation data</param>
    /// <returns>Created task</returns>
    Task<TaskDto> CreateTaskAsync(Guid userId, CreateTaskDto dto);

    /// <summary>
    /// Update an existing task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    /// <param name="dto">Task update data</param>
    /// <returns>Updated task</returns>
    Task<TaskDto> UpdateTaskAsync(Guid userId, Guid taskId, UpdateTaskDto dto);

    /// <summary>
    /// Archive a task (soft delete)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    Task ArchiveTaskAsync(Guid userId, Guid taskId);

    /// <summary>
    /// Restore an archived task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    Task RestoreTaskAsync(Guid userId, Guid taskId);

    // ============================================
    // Theme Management
    // ============================================

    /// <summary>
    /// Get all themes for a user, ordered by Order property
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of themes</returns>
    Task<List<TaskThemeDto>> GetThemesAsync(Guid userId);

    /// <summary>
    /// Create a new theme
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Theme creation data</param>
    /// <returns>Created theme</returns>
    Task<TaskThemeDto> CreateThemeAsync(Guid userId, CreateTaskThemeDto dto);

    /// <summary>
    /// Update an existing theme
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="themeId">Theme ID</param>
    /// <param name="dto">Theme update data</param>
    /// <returns>Updated theme</returns>
    Task<TaskThemeDto> UpdateThemeAsync(Guid userId, Guid themeId, UpdateTaskThemeDto dto);

    /// <summary>
    /// Delete a theme and move all its tasks to null theme (unassigned)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="themeId">Theme ID</param>
    Task DeleteThemeAsync(Guid userId, Guid themeId);

    /// <summary>
    /// Reorder themes by providing array of theme IDs in desired order
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="themeIds">Array of theme IDs in new order</param>
    Task ReorderThemesAsync(Guid userId, List<Guid> themeIds);

    /// <summary>
    /// Move a task to a different theme (or remove from theme by passing null)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    /// <param name="newThemeId">New theme ID (null to remove from theme)</param>
    Task MoveTaskToThemeAsync(Guid userId, Guid taskId, Guid? newThemeId);

    // ============================================
    // Subtask Management
    // ============================================

    /// <summary>
    /// Add a new subtask to a task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Parent task ID</param>
    /// <param name="dto">Subtask creation data</param>
    /// <returns>Created subtask</returns>
    Task<SubtaskDto> AddSubtaskAsync(Guid userId, Guid taskId, CreateSubtaskDto dto);

    /// <summary>
    /// Update an existing subtask
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Parent task ID</param>
    /// <param name="subtaskId">Subtask ID</param>
    /// <param name="dto">Subtask update data</param>
    /// <returns>Updated subtask</returns>
    Task<SubtaskDto> UpdateSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId, UpdateSubtaskDto dto);

    /// <summary>
    /// Delete a subtask
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Parent task ID</param>
    /// <param name="subtaskId">Subtask ID</param>
    Task DeleteSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId);

    /// <summary>
    /// Toggle subtask completion status
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Parent task ID</param>
    /// <param name="subtaskId">Subtask ID</param>
    /// <returns>Updated subtask</returns>
    Task<SubtaskDto> ToggleSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId);

    // ============================================
    // Recurrence Management
    // ============================================

    /// <summary>
    /// Set or update recurrence rule for a task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    /// <param name="dto">Recurrence data</param>
    /// <returns>Created/updated recurrence</returns>
    Task<RecurrenceDto> SetRecurrenceAsync(Guid userId, Guid taskId, CreateRecurrenceDto dto);

    /// <summary>
    /// Remove recurrence from a task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    Task RemoveRecurrenceAsync(Guid userId, Guid taskId);

    // ============================================
    // Tag Management
    // ============================================

    /// <summary>
    /// Add a tag to a task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    /// <param name="tagId">Tag ID</param>
    Task AddTagAsync(Guid userId, Guid taskId, Guid tagId);

    /// <summary>
    /// Remove a tag from a task
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    /// <param name="tagId">Tag ID</param>
    Task RemoveTagAsync(Guid userId, Guid taskId, Guid tagId);

    // ============================================
    // Ordering & Status Helpers
    // ============================================

    /// <summary>
    /// Reorder tasks (and optionally move between themes) in a single batch operation.
    /// Client sends the full desired state (TaskId, ThemeId, Order) for all affected tasks.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="items">Collection of tuples (TaskId, ThemeId, Order)</param>
    Task ReorderTasksAsync(Guid userId, List<(Guid TaskId, Guid? ThemeId, int Order)> items);

    /// <summary>
    /// Mark task as completed (Done) and set CompletedAt timestamp.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    Task CompleteTaskAsync(Guid userId, Guid taskId);

    /// <summary>
    /// Change task status (handles CompletedAt timestamp logic internally).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="taskId">Task ID</param>
    /// <param name="status">New status</param>
    Task ChangeStatusAsync(Guid userId, Guid taskId, Flowly.Domain.Enums.TasksStatus status);
}
