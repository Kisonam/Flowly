using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.DTOs.Tags;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tags")]
[Produces("application/json")]
public class TagsController(ITagService tagService, ILogger<TagsController> logger) : ControllerBase
{
    private readonly ITagService _tagService = tagService;
    private readonly ILogger<TagsController> _logger = logger;

    /// <summary>
    /// Get all tags for the current user
    /// </summary>
    /// <returns>List of tags</returns>
    /// <response code="200">Tags retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = GetCurrentUserId();
            var tags = await _tagService.GetAllAsync(userId);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tags");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tag = await _tagService.GetByIdAsync(userId, id);
            return Ok(tag);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Tag not found: {TagId}", id);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tag: {TagId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }


    [HttpPost]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tag = await _tagService.CreateAsync(userId, dto);

            _logger.LogInformation("Tag created: {TagId} by user: {UserId}", tag.Id, userId);

            return CreatedAtAction(nameof(GetById), new { id = tag.Id }, tag);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Tag creation failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tag");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    /// <summary>
    /// Update an existing tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="dto">Tag update data</param>
    /// <returns>Updated tag</returns>
    /// <response code="200">Tag updated successfully</response>
    /// <response code="404">Tag not found</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Not authenticated</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTagDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tag = await _tagService.UpdateAsync(userId, id, dto);

            _logger.LogInformation("Tag updated: {TagId} by user: {UserId}", id, userId);

            return Ok(tag);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Tag update failed: {Message}", ex.Message);
            
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ErrorResponse
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Path = Request.Path
                });
            }

            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tag: {TagId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    /// <summary>
    /// Delete a tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">Tag deleted successfully</response>
    /// <response code="404">Tag not found</response>
    /// <response code="401">Not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _tagService.DeleteAsync(userId, id);

            _logger.LogInformation("Tag deleted: {TagId} by user: {UserId}", id, userId);

            return Ok(new { message = "Tag deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Tag deletion failed: {Message}", ex.Message);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete tag: {TagId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
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
