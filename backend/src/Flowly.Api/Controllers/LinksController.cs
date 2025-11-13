using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Links;
using Flowly.Application.Interfaces;
using Flowly.Domain.Enums;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/links")]
[Produces("application/json")]
public class LinksController : ControllerBase
{
    private readonly ILinkService _linkService;
    private readonly ILogger<LinksController> _logger;

    public LinksController(ILinkService linkService, ILogger<LinksController> logger)
    {
        _linkService = linkService;
        _logger = logger;
    }

    // ============================================
    // POST /api/links
    // ============================================

    /// <summary>
    /// Create a new link between two entities
    /// </summary>
    /// <param name="dto">Link creation data</param>
    /// <returns>Created link with previews</returns>
    /// <response code="201">Link created successfully</response>
    /// <response code="400">Invalid request or validation error</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">One or both entities not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(LinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var link = await _linkService.CreateLinkAsync(userId, dto);
            
            return CreatedAtAction(nameof(GetLinksForEntity), 
                new { type = link.FromType.ToString(), id = link.FromId }, 
                link);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found when creating link");
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating link");
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating link");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while creating the link" });
        }
    }

    // ============================================
    // DELETE /api/links/{linkId}
    // ============================================

    /// <summary>
    /// Delete a link by ID
    /// </summary>
    /// <param name="linkId">Link ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Link deleted successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Link not found</response>
    [HttpDelete("{linkId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLink(Guid linkId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _linkService.DeleteLinkAsync(userId, linkId);
            
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Link not found when deleting: {LinkId}", linkId);
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting link: {LinkId}", linkId);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while deleting the link" });
        }
    }

    // ============================================
    // GET /api/links?type={type}&id={id}
    // ============================================

    /// <summary>
    /// Get all links for a specific entity
    /// </summary>
    /// <param name="type">Entity type (Note, Task, Transaction)</param>
    /// <param name="id">Entity ID</param>
    /// <returns>List of links with previews</returns>
    /// <response code="200">Links retrieved successfully</response>
    /// <response code="400">Invalid entity type or missing parameters</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Entity not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<LinkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinksForEntity([FromQuery] string? type, [FromQuery] Guid? id)
    {
        try
        {
            // Validate parameters
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(new ErrorResponse { Message = "Parameter 'type' is required" });
            }

            if (!id.HasValue || id.Value == Guid.Empty)
            {
                return BadRequest(new ErrorResponse { Message = "Parameter 'id' is required and must be a valid GUID" });
            }

            // Parse entity type
            if (!Enum.TryParse<LinkEntityType>(type, ignoreCase: true, out var parsedEntityType))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = $"Invalid entity type: {type}. Valid values are: Note, Task, Transaction" 
                });
            }

            var userId = GetCurrentUserId();
            var links = await _linkService.GetLinksForEntityAsync(userId, parsedEntityType, id.Value);
            
            return Ok(links);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found when getting links: {Type}/{Id}", type, id);
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting links for entity: {Type}/{Id}", type, id);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while retrieving links" });
        }
    }

    // ============================================
    // GET /api/links/preview?type={type}&id={id}
    // ============================================

    /// <summary>
    /// Get a preview of an entity (for displaying in link previews)
    /// </summary>
    /// <param name="type">Entity type (Note, Task, Transaction)</param>
    /// <param name="id">Entity ID</param>
    /// <returns>Entity preview</returns>
    /// <response code="200">Preview retrieved successfully</response>
    /// <response code="400">Invalid entity type or missing parameters</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Entity not found</response>
    [HttpGet("preview")]
    [ProducesResponseType(typeof(EntityPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview([FromQuery] string? type, [FromQuery] Guid? id)
    {
        try
        {
            // Validate parameters
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(new ErrorResponse { Message = "Parameter 'type' is required" });
            }

            if (!id.HasValue || id.Value == Guid.Empty)
            {
                return BadRequest(new ErrorResponse { Message = "Parameter 'id' is required and must be a valid GUID" });
            }

            // Parse entity type
            if (!Enum.TryParse<LinkEntityType>(type, ignoreCase: true, out var parsedEntityType))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = $"Invalid entity type: {type}. Valid values are: Note, Task, Transaction" 
                });
            }

            var userId = GetCurrentUserId();
            var preview = await _linkService.GetPreviewAsync(userId, parsedEntityType, id.Value);
            
            return Ok(preview);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found when getting preview: {Type}/{Id}", type, id);
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preview for entity: {Type}/{Id}", type, id);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while retrieving the preview" });
        }
    }

    // ============================================
    // Helper Methods
    // ============================================

    /// <summary>
    /// Get current user ID from JWT claims
    /// </summary>
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
