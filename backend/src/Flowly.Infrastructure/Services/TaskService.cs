using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.DTOs.Tasks;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _dbContext;

    public TaskService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ============================================
    // Task CRUD Operations
    // ============================================

    public async Task<PagedResult<TaskDto>> GetAllTasksAsync(Guid userId, TaskFilterDto filter)
    {
        var query = _dbContext.Tasks
            .Include(t => t.TaskTheme)
            .Include(t => t.Subtasks)
            .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
            .Include(t => t.Recurrence)
            .Where(t => t.UserId == userId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchTerm) || 
                                   (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
        }

        if (filter.TagIds != null && filter.TagIds.Any())
        {
            query = query.Where(t => t.TaskTags.Any(tt => filter.TagIds.Contains(tt.TagId)));
        }

        if (filter.ThemeIds != null && filter.ThemeIds.Any())
        {
            query = query.Where(t => t.TaskThemeId.HasValue && filter.ThemeIds.Contains(t.TaskThemeId.Value));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        if (filter.IsArchived.HasValue)
        {
            query = query.Where(t => t.IsArchived == filter.IsArchived.Value);
        }

        if (filter.DueDateOn.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.DueDateOn.Value, DateTimeKind.Utc);
            var end = start.AddDays(1);
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= start && t.DueDate.Value < end);
        }
        else if (filter.DueDateTo.HasValue)
        {
            var endExclusive = DateTime.SpecifyKind(filter.DueDateTo.Value, DateTimeKind.Utc);
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= endExclusive);
        }

        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != Domain.Enums.TasksStatus.Done);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        // Order: theme grouping + explicit Order inside theme, fallback UpdatedAt for stable ordering
        var tasks = await query
            .OrderBy(t => t.TaskThemeId.HasValue ? 0 : 1) // Unassigned first
            .ThenBy(t => t.TaskThemeId) // Group by theme
            .ThenBy(t => t.Order)
            .ThenByDescending(t => t.UpdatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // Map to DTOs
        var taskDtos = tasks.Select(MapToTaskDto).ToList();

        return new PagedResult<TaskDto>
        {
            Items = taskDtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<TaskDto> GetTaskByIdAsync(Guid userId, Guid taskId)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.TaskTheme)
            .Include(t => t.Subtasks.OrderBy(s => s.Order))
            .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
            .Include(t => t.Recurrence)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        return MapToTaskDto(task);
    }

    public async Task<TaskDto> CreateTaskAsync(Guid userId, CreateTaskDto dto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

        // Verify theme if provided
        if (dto.ThemeId.HasValue)
        {
            var themeExists = await _dbContext.TaskThemes
                .AnyAsync(t => t.Id == dto.ThemeId.Value && t.UserId == userId);

            if (!themeExists)
            {
                throw new InvalidOperationException("Theme not found");
            }
        }

        // Create task entity
        // Determine order within theme (or unassigned)
        var baseQuery = _dbContext.Tasks.Where(t => t.UserId == userId && t.TaskThemeId == dto.ThemeId);
        var maxOrder = await baseQuery.MaxAsync(t => (int?)t.Order) ?? -1;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TaskThemeId = dto.ThemeId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            DueDate = NormalizeToUtcDate(dto.DueDate),
            Color = dto.Color,
            Priority = dto.Priority ?? Domain.Enums.TaskPriority.None,
            Status = Domain.Enums.TasksStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Order = maxOrder + 1
        };

        _dbContext.Tasks.Add(task);

        // Add tags if provided
        if (dto.TagIds != null && dto.TagIds.Any())
        {
            await AddTagsToTaskAsync(task.Id, userId, dto.TagIds);
        }

        await _dbContext.SaveChangesAsync();

        // Reload with relations
        return await GetTaskByIdAsync(userId, task.Id);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid userId, Guid taskId, UpdateTaskDto dto)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.TaskTags)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Validate title
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

        // Verify theme if provided
        if (dto.ThemeId.HasValue)
        {
            var themeExists = await _dbContext.TaskThemes
                .AnyAsync(t => t.Id == dto.ThemeId.Value && t.UserId == userId);

            if (!themeExists)
            {
                throw new InvalidOperationException("Theme not found");
            }
        }

        // Update fields
        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
    task.DueDate = NormalizeToUtcDate(dto.DueDate);
        task.Color = dto.Color;
        task.Priority = dto.Priority;
        task.TaskThemeId = dto.ThemeId;
        
        // Update status and handle completion
        if (task.Status != dto.Status)
        {
            task.ChangeStatus(dto.Status);
        }

        task.UpdatedAt = DateTime.UtcNow;

        // Update tags if provided
        if (dto.TagIds != null)
        {
            // Remove existing tags
            var existingTags = task.TaskTags.ToList();
            _dbContext.TaskTags.RemoveRange(existingTags);

            // Add new tags
            if (dto.TagIds.Any())
            {
                await AddTagsToTaskAsync(taskId, userId, dto.TagIds);
            }
        }

        await _dbContext.SaveChangesAsync();

        return await GetTaskByIdAsync(userId, taskId);
    }

    // ============================================
    // Date helpers
    // ============================================
    private static DateTime? NormalizeToUtcDate(DateTime? source)
    {
        if (!source.HasValue) return null;
        var dt = source.Value;
        if (dt.Kind == DateTimeKind.Utc) return dt;
        if (dt.Kind == DateTimeKind.Local)
        {
            return dt.ToUniversalTime();
        }
        // Unspecified: treat as local time and convert to UTC
        var unspecifiedAsLocal = DateTime.SpecifyKind(dt, DateTimeKind.Local);
        return unspecifiedAsLocal.ToUniversalTime();
    }

    public async Task ArchiveTaskAsync(Guid userId, Guid taskId)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Recurrence)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Archive and remove recurrence if present to avoid ghost scheduling
        task.Archive();
        if (task.Recurrence != null)
        {
            _dbContext.TaskRecurrences.Remove(task.Recurrence);
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task RestoreTaskAsync(Guid userId, Guid taskId)
    {
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        task.Restore();
        await _dbContext.SaveChangesAsync();
    }

    // ============================================
    // Theme Management
    // ============================================

    public async Task<List<TaskThemeDto>> GetThemesAsync(Guid userId)
    {
        var themes = await _dbContext.TaskThemes
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        return themes.Select(MapToThemeDto).ToList();
    }

    public async Task<TaskThemeDto> CreateThemeAsync(Guid userId, CreateTaskThemeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

        // Get max order
        var maxOrder = await _dbContext.TaskThemes
            .Where(t => t.UserId == userId)
            .MaxAsync(t => (int?)t.Order) ?? -1;

        var theme = new TaskTheme
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title.Trim(),
            Color = dto.Color,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskThemes.Add(theme);
        await _dbContext.SaveChangesAsync();

        return MapToThemeDto(theme);
    }

    public async Task<TaskThemeDto> UpdateThemeAsync(Guid userId, Guid themeId, UpdateTaskThemeDto dto)
    {
        var theme = await _dbContext.TaskThemes
            .FirstOrDefaultAsync(t => t.Id == themeId && t.UserId == userId);

        if (theme == null)
        {
            throw new InvalidOperationException("Theme not found");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            theme.UpdateTitle(dto.Title);
        }

        if (dto.Color != null)
        {
            theme.UpdateColor(dto.Color);
        }

        if (dto.Order.HasValue)
        {
            theme.UpdateOrder(dto.Order.Value);
        }

        await _dbContext.SaveChangesAsync();

        return MapToThemeDto(theme);
    }

    public async Task DeleteThemeAsync(Guid userId, Guid themeId)
    {
        var theme = await _dbContext.TaskThemes
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.Id == themeId && t.UserId == userId);

        if (theme == null)
        {
            throw new InvalidOperationException("Theme not found");
        }

        // Move all tasks to null theme
        foreach (var task in theme.Tasks)
        {
            task.MoveToTheme(null);
        }

        _dbContext.TaskThemes.Remove(theme);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ReorderThemesAsync(Guid userId, List<Guid> themeIds)
    {
        // Verify all themes belong to user
        var themes = await _dbContext.TaskThemes
            .Where(t => t.UserId == userId && themeIds.Contains(t.Id))
            .ToListAsync();

        if (themes.Count != themeIds.Count)
        {
            throw new InvalidOperationException("One or more themes not found");
        }

        // Update order
        for (int i = 0; i < themeIds.Count; i++)
        {
            var theme = themes.First(t => t.Id == themeIds[i]);
            theme.UpdateOrder(i);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task MoveTaskToThemeAsync(Guid userId, Guid taskId, Guid? newThemeId)
    {
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Verify new theme if provided
        if (newThemeId.HasValue)
        {
            var themeExists = await _dbContext.TaskThemes
                .AnyAsync(t => t.Id == newThemeId.Value && t.UserId == userId);

            if (!themeExists)
            {
                throw new InvalidOperationException("Theme not found");
            }
        }

        // Recalculate order in new theme
        if (task.TaskThemeId != newThemeId)
        {
            var maxOrder = await _dbContext.Tasks
                .Where(t => t.UserId == userId && t.TaskThemeId == newThemeId && t.Id != task.Id)
                .MaxAsync(t => (int?)t.Order) ?? -1;
            task.MoveToTheme(newThemeId);
            task.Order = maxOrder + 1;
        }
        else
        {
            task.MoveToTheme(newThemeId);
        }
        await _dbContext.SaveChangesAsync();
    }

    // ============================================
    // Subtask Management
    // ============================================

    public async Task<SubtaskDto> AddSubtaskAsync(Guid userId, Guid taskId, CreateSubtaskDto dto)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Subtasks)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

        // Get max order
        var maxOrder = task.Subtasks.Any() 
            ? task.Subtasks.Max(s => s.Order) 
            : -1;

        var subtask = new TaskSubtask
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId,
            Title = dto.Title.Trim(),
            Order = maxOrder + 1,
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskSubtasks.Add(subtask);
        
        // Update parent task timestamp
        task.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();

        return MapToSubtaskDto(subtask);
    }

    public async Task<SubtaskDto> UpdateSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId, UpdateSubtaskDto dto)
    {
        // Verify task belongs to user
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        var subtask = await _dbContext.TaskSubtasks
            .FirstOrDefaultAsync(s => s.Id == subtaskId && s.TaskItemId == taskId);

        if (subtask == null)
        {
            throw new InvalidOperationException("Subtask not found");
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

        subtask.UpdateTitle(dto.Title);

        if (dto.IsDone != subtask.IsDone)
        {
            if (dto.IsDone)
            {
                subtask.MarkAsDone();
            }
            else
            {
                subtask.MarkAsNotDone();
            }
        }

        // Update parent task timestamp
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return MapToSubtaskDto(subtask);
    }

    public async Task DeleteSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId)
    {
        // Verify task belongs to user
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        var subtask = await _dbContext.TaskSubtasks
            .FirstOrDefaultAsync(s => s.Id == subtaskId && s.TaskItemId == taskId);

        if (subtask == null)
        {
            throw new InvalidOperationException("Subtask not found");
        }

        _dbContext.TaskSubtasks.Remove(subtask);
        
        // Update parent task timestamp
        task.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
    }

    public async Task<SubtaskDto> ToggleSubtaskAsync(Guid userId, Guid taskId, Guid subtaskId)
    {
        // Verify task belongs to user
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        var subtask = await _dbContext.TaskSubtasks
            .FirstOrDefaultAsync(s => s.Id == subtaskId && s.TaskItemId == taskId);

        if (subtask == null)
        {
            throw new InvalidOperationException("Subtask not found");
        }

        subtask.Toggle();
        
        // Update parent task timestamp
        task.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();

        return MapToSubtaskDto(subtask);
    }

    // ============================================
    // Recurrence Management
    // ============================================

    public async Task<RecurrenceDto> SetRecurrenceAsync(Guid userId, Guid taskId, CreateRecurrenceDto dto)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Recurrence)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (string.IsNullOrWhiteSpace(dto.Rule))
        {
            throw new ArgumentException("Recurrence rule is required", nameof(dto.Rule));
        }

        // Update existing or create new
        if (task.Recurrence != null)
        {
            task.Recurrence.UpdateRule(dto.Rule);
        }
        else
        {
            var recurrence = new TaskRecurrence
            {
                Id = Guid.NewGuid(),
                TaskItemId = taskId,
                Rule = dto.Rule.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.TaskRecurrences.Add(recurrence);
            task.Recurrence = recurrence;
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return MapToRecurrenceDto(task.Recurrence!);
    }

    public async Task RemoveRecurrenceAsync(Guid userId, Guid taskId)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Recurrence)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (task.Recurrence != null)
        {
            _dbContext.TaskRecurrences.Remove(task.Recurrence);
            task.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    // ============================================
    // Tag Management
    // ============================================

    public async Task AddTagAsync(Guid userId, Guid taskId, Guid tagId)
    {
        // Verify task belongs to user
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Verify tag belongs to user
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

        if (tag == null)
        {
            throw new InvalidOperationException("Tag not found");
        }

        // Check if tag already added
        var existingTaskTag = await _dbContext.TaskTags
            .FirstOrDefaultAsync(tt => tt.TaskId == taskId && tt.TagId == tagId);

        if (existingTaskTag != null)
        {
            return; // Already exists, no need to add
        }

        // Add tag
        var taskTag = new TaskTag
        {
            TaskId = taskId,
            TagId = tagId
        };

        _dbContext.TaskTags.Add(taskTag);
        
        // Update task timestamp
        task.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveTagAsync(Guid userId, Guid taskId, Guid tagId)
    {
        // Verify task belongs to user
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        var taskTag = await _dbContext.TaskTags
            .FirstOrDefaultAsync(tt => tt.TaskId == taskId && tt.TagId == tagId);

        if (taskTag == null)
        {
            throw new InvalidOperationException("Tag not found on task");
        }

        _dbContext.TaskTags.Remove(taskTag);
        
        // Update task timestamp
        task.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
    }

    // ============================================
    // Private Helper Methods
    // ============================================

    private async Task AddTagsToTaskAsync(Guid taskId, Guid userId, List<Guid> tagIds)
    {
        // Verify all tags belong to user
        var tags = await _dbContext.Tags
            .Where(t => tagIds.Contains(t.Id) && t.UserId == userId)
            .ToListAsync();

        if (tags.Count != tagIds.Count)
        {
            throw new InvalidOperationException("One or more tags not found");
        }

        // Add task-tag relations
        var taskTags = tagIds.Select(tagId => new TaskTag
        {
            TaskId = taskId,
            TagId = tagId
        }).ToList();

        _dbContext.TaskTags.AddRange(taskTags);
    }

    private TaskDto MapToTaskDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Status = task.Status,
            Priority = task.Priority,
            Color = task.Color,
            IsArchived = task.IsArchived,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            CompletedAt = task.CompletedAt,
            Order = task.Order,
            Theme = task.TaskTheme != null ? MapToThemeDto(task.TaskTheme) : null,
            Subtasks = task.Subtasks
                .OrderBy(s => s.Order)
                .Select(MapToSubtaskDto)
                .ToList(),
            Tags = task.TaskTags.Select(tt => new TagDto
            {
                Id = tt.Tag.Id,
                Name = tt.Tag.Name,
                Color = tt.Tag.Color
            }).ToList(),
            Recurrence = task.Recurrence != null ? MapToRecurrenceDto(task.Recurrence) : null,
            IsOverdue = task.IsOverdue()
        };
    }

    // ============================================
    // Ordering / Status helpers
    // ============================================

    public async Task ReorderTasksAsync(Guid userId, List<(Guid TaskId, Guid? ThemeId, int Order)> items)
    {
        var taskIds = items.Select(i => i.TaskId).ToList();
        var tasks = await _dbContext.Tasks.Where(t => t.UserId == userId && taskIds.Contains(t.Id)).ToListAsync();

        // Verify all tasks fetched
        if (tasks.Count != items.Count)
            throw new InvalidOperationException("One or more tasks not found for reorder");

        foreach (var entry in items)
        {
            var task = tasks.First(t => t.Id == entry.TaskId);
            // If theme changed we don't auto-shift others here, assumption: client sends full desired state
            task.TaskThemeId = entry.ThemeId;
            task.Order = entry.Order;
            task.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task CompleteTaskAsync(Guid userId, Guid taskId)
    {
        var task = await _dbContext.Tasks
            .Include(t => t.Recurrence)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task == null) throw new InvalidOperationException("Task not found");
        
        // Prevent accidental double processing: if already Done and CompletedAt set a moment ago, do nothing
        if (task.Status == Domain.Enums.TasksStatus.Done && task.CompletedAt.HasValue && (DateTime.UtcNow - task.CompletedAt.Value) < TimeSpan.FromSeconds(2))
        {
            return;
        }

        task.Complete();

        // Rolling recurrence: if rule exists, advance DueDate by FREQ while preserving time-of-day
        if (task.Recurrence != null)
        {
            var rule = task.Recurrence.Rule?.Trim().ToUpperInvariant() ?? string.Empty;

            // Base next date from existing due date if any, otherwise from now (date part only)
            var baseDateTime = task.DueDate ?? DateTime.UtcNow;
            var timeOfDay = baseDateTime.TimeOfDay;
            var baseDate = baseDateTime.Date;

            DateTime nextDate;
            if (rule.Contains("FREQ=DAILY"))
            {
                nextDate = baseDate.AddDays(1);
            }
            else if (rule.Contains("FREQ=WEEKLY"))
            {
                nextDate = baseDate.AddDays(7);
            }
            else if (rule.Contains("FREQ=MONTHLY"))
            {
                nextDate = baseDate.AddMonths(1);
            }
            else
            {
                // Default/fallback: daily
                nextDate = baseDate.AddDays(1);
            }

            // Update recurrence metadata
            task.Recurrence.LastOccurrence = DateTime.UtcNow;
            task.Recurrence.NextOccurrence = nextDate + timeOfDay;

            // Restart same task for next occurrence (rolling): set to Todo, clear completion, and move due date forward
            task.Status = Domain.Enums.TasksStatus.Todo;
            task.CompletedAt = null;
            task.DueDate = nextDate + timeOfDay; // preserve time component
            task.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task ChangeStatusAsync(Guid userId, Guid taskId, Domain.Enums.TasksStatus status)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task == null) throw new InvalidOperationException("Task not found");
        task.ChangeStatus(status);
        await _dbContext.SaveChangesAsync();
    }

    private static TaskThemeDto MapToThemeDto(TaskTheme theme)
    {
        return new TaskThemeDto
        {
            Id = theme.Id,
            Title = theme.Title,
            Order = theme.Order,
            Color = theme.Color
        };
    }

    private static SubtaskDto MapToSubtaskDto(TaskSubtask subtask)
    {
        return new SubtaskDto
        {
            Id = subtask.Id,
            Title = subtask.Title,
            IsDone = subtask.IsDone,
            Order = subtask.Order,
            CreatedAt = subtask.CreatedAt,
            CompletedAt = subtask.CompletedAt
        };
    }

    private static RecurrenceDto MapToRecurrenceDto(TaskRecurrence recurrence)
    {
        return new RecurrenceDto
        {
            Id = recurrence.Id,
            Rule = recurrence.Rule,
            CreatedAt = recurrence.CreatedAt,
            LastOccurrence = recurrence.LastOccurrence,
            NextOccurrence = recurrence.NextOccurrence
        };
    }
}
