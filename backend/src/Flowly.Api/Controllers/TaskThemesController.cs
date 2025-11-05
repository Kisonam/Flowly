using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.Interfaces;
using Flowly.Application.DTOs.Tasks;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/task-themes")]
[Produces("application/json")]
public class TaskThemesController(ITaskThemeService service, ILogger<TaskThemesController> logger) : ControllerBase
{
    private readonly ITaskThemeService _service = service;
    private readonly ILogger<TaskThemesController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(List<TaskThemeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = GetUserId();
            var themes = await _service.GetAllAsync(userId);
            return Ok(themes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task themes");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskThemeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTaskThemeDto dto)
    {
        try
        {
            var userId = GetUserId();
            var theme = await _service.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetAll), new { id = theme.Id }, theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task theme");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskThemeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskThemeDto dto)
    {
        try
        {
            var userId = GetUserId();
            var theme = await _service.UpdateAsync(userId, id, dto);
            return Ok(theme);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task theme {ThemeId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await _service.DeleteAsync(userId, id);
            return Ok(new { message = "Theme deleted" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task theme {ThemeId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }
}
