using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Tasks;
using Flowly.Application.Interfaces;
using Flowly.Domain.Enums;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? tagIds = null,
        [FromQuery] string? themeIds = null,
        [FromQuery] TasksStatus? status = null,
        [FromQuery] TaskPriority? priority = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] bool? isOverdue = null,
        [FromQuery] DateTime? dueDateOn = null,
        [FromQuery] DateTime? dueDateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            List<Guid>? tagIdList = null;
            if (!string.IsNullOrWhiteSpace(tagIds))
            {
                tagIdList = tagIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
            }

            List<Guid>? themeIdList = null;
            if (!string.IsNullOrWhiteSpace(themeIds))
            {
                themeIdList = themeIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
            }

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var filter = new TaskFilterDto
            {
                Search = search,
                TagIds = tagIdList,
                ThemeIds = themeIdList,
                Status = status,
                Priority = priority,
                IsArchived = isArchived,
                IsOverdue = isOverdue,
                DueDateOn = dueDateOn,
                DueDateTo = dueDateTo,
                Page = page,
                PageSize = pageSize
            };

            var userId = GetCurrentUserId();
            var result = await _taskService.GetAllTasksAsync(userId, filter);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while getting tasks");
            return Unauthorized(new ErrorResponse
            {
                StatusCode = 401,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.GetTaskByIdAsync(userId, id);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        
        try
        {
            var dtoJson = System.Text.Json.JsonSerializer.Serialize(dto);
            _logger.LogInformation("Incoming CreateTaskDto: {DtoJson}", dtoJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize incoming CreateTaskDto for logging");
        }

        if (!ModelState.IsValid)
        {
            
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .Select(x => new {
                    Field = x.Key,
                    Errors = x.Value != null ? x.Value.Errors.Select(e => e.ErrorMessage).ToList() : new List<string>()
                });
            var errorsJson = System.Text.Json.JsonSerializer.Serialize(errors);
            _logger.LogError("ModelState validation failed: {Errors}", errorsJson);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Validation failed", Path = Request.Path });
        }

        try
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.CreateTaskAsync(userId, dto);
            _logger.LogInformation("Task created: {TaskId} by user: {UserId}", task.Id, userId);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task");
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.UpdateTaskAsync(userId, id, dto);
            _logger.LogInformation("Task updated: {TaskId} by user: {UserId}", id, userId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.ArchiveTaskAsync(userId, id);
            _logger.LogInformation("Task archived: {TaskId} by user: {UserId}", id, userId);
            return Ok(new { message = "Task archived successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive task: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpGet("themes")]
    [ProducesResponseType(typeof(List<TaskThemeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThemes()
    {
        try
        {
            var userId = GetCurrentUserId();
            var themes = await _taskService.GetThemesAsync(userId);
            return Ok(themes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get themes");
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderTasks([FromBody] ReorderTasksDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var items = dto.Items.Select(i => (i.TaskId, i.ThemeId, i.Order)).ToList();
            await _taskService.ReorderTasksAsync(userId, items);
            _logger.LogInformation("Tasks reordered by user: {UserId}. Items: {Count}", userId, items.Count);
            return Ok(new { message = "Tasks reordered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder tasks");
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.CompleteTaskAsync(userId, id);
            _logger.LogInformation("Task completed: {TaskId} by user: {UserId}", id, userId);
            return Ok(new { message = "Task marked as done" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete task: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("{id}/status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(Guid id, TasksStatus status)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.ChangeStatusAsync(userId, id, status);
            _logger.LogInformation("Task status changed: {TaskId} -> {Status}", id, status);
            return Ok(new { message = "Task status updated" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change task status: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("themes")]
    [ProducesResponseType(typeof(TaskThemeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTheme([FromBody] CreateTaskThemeDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var theme = await _taskService.CreateThemeAsync(userId, dto);
            _logger.LogInformation("Theme created: {ThemeId} by user: {UserId}", theme.Id, userId);
            return CreatedAtAction(nameof(GetThemes), null, theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create theme");
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPut("themes/{id}")]
    [ProducesResponseType(typeof(TaskThemeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTheme(Guid id, [FromBody] UpdateTaskThemeDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var theme = await _taskService.UpdateThemeAsync(userId, id, dto);
            _logger.LogInformation("Theme updated: {ThemeId} by user: {UserId}", id, userId);
            return Ok(theme);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update theme: {ThemeId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpDelete("themes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTheme(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.DeleteThemeAsync(userId, id);
            _logger.LogInformation("Theme deleted: {ThemeId} by user: {UserId}", id, userId);
            return Ok(new { message = "Theme deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete theme: {ThemeId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("themes/reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderThemes([FromBody] List<Guid> themeIds)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.ReorderThemesAsync(userId, themeIds);
            _logger.LogInformation("Themes reordered by user: {UserId}", userId);
            return Ok(new { message = "Themes reordered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder themes");
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("{id}/move/{themeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveToTheme(Guid id, string themeId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            Guid? targetThemeId = null;
            if (!string.IsNullOrWhiteSpace(themeId) && themeId.ToLower() != "null" && 
                Guid.TryParse(themeId, out var parsedGuid) && parsedGuid != Guid.Empty)
            {
                targetThemeId = parsedGuid;
            }

            await _taskService.MoveTaskToThemeAsync(userId, id, targetThemeId);
            _logger.LogInformation("Task {TaskId} moved to theme {ThemeId}", id, targetThemeId);
            return Ok(new { message = "Task moved successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move task: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("{id}/subtasks")]
    [ProducesResponseType(typeof(SubtaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSubtask(Guid id, [FromBody] CreateSubtaskDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var subtask = await _taskService.AddSubtaskAsync(userId, id, dto);
            _logger.LogInformation("Subtask added to task {TaskId}", id);
            return CreatedAtAction(nameof(GetById), new { id }, subtask);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add subtask to task: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPut("{id}/subtasks/{subtaskId}")]
    [ProducesResponseType(typeof(SubtaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubtask(Guid id, Guid subtaskId, [FromBody] UpdateSubtaskDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var subtask = await _taskService.UpdateSubtaskAsync(userId, id, subtaskId, dto);
            _logger.LogInformation("Subtask {SubtaskId} updated in task {TaskId}", subtaskId, id);
            return Ok(subtask);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subtask {SubtaskId}", subtaskId);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpDelete("{id}/subtasks/{subtaskId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubtask(Guid id, Guid subtaskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.DeleteSubtaskAsync(userId, id, subtaskId);
            _logger.LogInformation("Subtask {SubtaskId} deleted from task {TaskId}", subtaskId, id);
            return Ok(new { message = "Subtask deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subtask {SubtaskId}", subtaskId);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPut("{id}/recurrence")]
    [ProducesResponseType(typeof(RecurrenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRecurrence(Guid id, [FromBody] CreateRecurrenceDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var recurrence = await _taskService.SetRecurrenceAsync(userId, id, dto);
            _logger.LogInformation("Recurrence set for task {TaskId}", id);
            return Ok(recurrence);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set recurrence for task: {TaskId}", id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpPost("{id}/tags/{tagId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTag(Guid id, Guid tagId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.AddTagAsync(userId, id, tagId);
            _logger.LogInformation("Tag {TagId} added to task {TaskId}", tagId, id);
            return Ok(new { message = "Tag added successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tag {TagId} to task {TaskId}", tagId, id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    [HttpDelete("{id}/tags/{tagId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag(Guid id, Guid tagId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _taskService.RemoveTagAsync(userId, id, tagId);
            _logger.LogInformation("Tag {TagId} removed from task {TaskId}", tagId, id);
            return Ok(new { message = "Tag removed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = ex.Message, Path = Request.Path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove tag {TagId} from task {TaskId}", tagId, id);
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = ex.Message, Path = Request.Path });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }
}
