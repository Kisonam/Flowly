using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.Interfaces;
using System.Security.Claims;
using System.Text;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notes")]
[Produces("application/json")]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INoteService noteService, ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? tagIds = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();

            List<Guid>? tagIdList = null;
            if (!string.IsNullOrWhiteSpace(tagIds))
            {
                tagIdList = tagIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
            }

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var filter = new NoteFilterDto
            {
                Search = search,
                TagIds = tagIdList,
                IsArchived = isArchived,
                Page = page,
                PageSize = pageSize
            };

            var result = await _noteService.GetAllAsync(userId, filter);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notes");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var note = await _noteService.GetByIdAsync(userId, id);

            return Ok(note);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Note not found: {NoteId}", id);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get note: {NoteId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var note = await _noteService.CreateAsync(userId, dto);

            _logger.LogInformation("Note created: {NoteId} by user: {UserId}", note.Id, userId);

            return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Note creation validation failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create note");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var note = await _noteService.UpdateAsync(userId, id, dto);

            _logger.LogInformation("Note updated: {NoteId} by user: {UserId}", id, userId);

            return Ok(note);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Note not found for update: {NoteId}", id);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update note: {NoteId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _noteService.ArchiveAsync(userId, id);

            _logger.LogInformation("Note archived: {NoteId} by user: {UserId}", id, userId);

            return Ok(new { message = "Note archived successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Note not found for archiving: {NoteId}", id);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive note: {NoteId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Restore(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _noteService.RestoreAsync(userId, id);

            _logger.LogInformation("Note restored: {NoteId} by user: {UserId}", id, userId);

            return Ok(new { message = "Note restored successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Note not found for restoring: {NoteId}", id);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore note: {NoteId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost("{id}/tags/{tagId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddTag(Guid id, Guid tagId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _noteService.AddTagAsync(userId, id, tagId);

            _logger.LogInformation("Tag {TagId} added to note {NoteId} by user: {UserId}", tagId, id, userId);

            return Ok(new { message = "Tag added successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to add tag: {Message}", ex.Message);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tag {TagId} to note {NoteId}", tagId, id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpDelete("{id}/tags/{tagId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveTag(Guid id, Guid tagId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _noteService.RemoveTagAsync(userId, id, tagId);

            _logger.LogInformation("Tag {TagId} removed from note {NoteId} by user: {UserId}", tagId, id, userId);

            return Ok(new { message = "Tag removed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to remove tag: {Message}", ex.Message);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove tag {TagId} from note {NoteId}", tagId, id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost("{id}/media")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadMedia(Guid id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = 400,
                    Message = "No file uploaded",
                    Path = Request.Path
                });
            }

            var userId = GetCurrentUserId();

            using var stream = file.OpenReadStream();
            var mediaUrl = await _noteService.UploadMediaAsync(
                userId,
                id,
                stream,
                file.FileName,
                file.ContentType
            );

            _logger.LogInformation("Media uploaded to note {NoteId} by user: {UserId}", id, userId);

            return Ok(new { mediaUrl });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to upload media: {Message}", ex.Message);
            
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
            _logger.LogError(ex, "Failed to upload media to note {NoteId}", id);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpGet("{id}/export")]
    [Produces("text/markdown")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Export(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var markdown = await _noteService.ExportMarkdownAsync(userId, id);

            var note = await _noteService.GetByIdAsync(userId, id);
            var fileName = $"{SanitizeFileName(note.Title)}.md";

            _logger.LogInformation("Note exported: {NoteId} by user: {UserId}", id, userId);

            var bytes = Encoding.UTF8.GetBytes(markdown);
            return File(bytes, "text/markdown", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Note not found for export: {NoteId}", id);
            return NotFound(new ErrorResponse
            {
                StatusCode = 404,
                Message = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export note: {NoteId}", id);
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

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "note" : sanitized;
    }
}
