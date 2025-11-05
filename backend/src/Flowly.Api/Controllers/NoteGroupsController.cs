using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.Interfaces;
using Flowly.Application.DTOs.Notes.Groups;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/note-groups")]
[Produces("application/json")]
public class NoteGroupsController(INoteGroupService service, ILogger<NoteGroupsController> logger) : ControllerBase
{
    private readonly INoteGroupService _service = service;
    private readonly ILogger<NoteGroupsController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(List<NoteGroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = GetUserId();
            var groups = await _service.GetAllAsync(userId);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get note groups");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteGroupDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateNoteGroupDto dto)
    {
        try
        {
            var userId = GetUserId();
            var group = await _service.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetAll), new { id = group.Id }, group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create note group");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(NoteGroupDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteGroupDto dto)
    {
        try
        {
            var userId = GetUserId();
            var group = await _service.UpdateAsync(userId, id, dto);
            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update note group {GroupId}", id);
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
            return Ok(new { message = "Group deleted" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete note group {GroupId}", id);
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
