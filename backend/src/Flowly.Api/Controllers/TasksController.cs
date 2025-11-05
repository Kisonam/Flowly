using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Tasks;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(ITaskItemQueryService taskService, ILogger<TasksController> logger) : ControllerBase
{
    private readonly ITaskItemQueryService _taskService = taskService;
    private readonly ILogger<TasksController> _logger = logger;

    /// <summary>
    /// List tasks for current user (lightweight)
    /// </summary>
    /// <param name="search">Search by title</param>
    /// <param name="isArchived">Filter by archive status</param>
    /// <param name="take">Max items (1..100, default 50)</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<TaskListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool? isArchived = null, [FromQuery] int? take = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var items = await _taskService.GetListAsync(userId, search, isArchived, take ?? 50);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tasks");
            return BadRequest(new { message = ex.Message });
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
