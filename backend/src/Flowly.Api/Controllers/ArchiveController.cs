using Flowly.Application.DTOs.Archive;
using Flowly.Application.Interfaces;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/archive")]
[Produces("application/json")]
public class ArchiveController : ControllerBase
{
    private readonly IArchiveService _archiveService;
    private readonly ILogger<ArchiveController> _logger;
    private readonly ArchiveMigrationService _migrationService;

    public ArchiveController(
        IArchiveService archiveService, 
        ILogger<ArchiveController> logger,
        ArchiveMigrationService migrationService)
    {
        _archiveService = archiveService;
        _logger = logger;
        _migrationService = migrationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ArchiveListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetArchived(
        [FromQuery] LinkEntityType? entityType = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "ArchivedAt",
        [FromQuery] string sortDirection = "desc")
    {
        try
        {
            var userId = GetCurrentUserId();
            var query = new ArchiveQueryDto
            {
                EntityType = entityType,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var result = await _archiveService.GetArchivedAsync(userId, query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve archived entities");
            return StatusCode(500, new { message = "Failed to retrieve archived entities" });
        }
    }

    [HttpGet("{archiveEntryId}/detail")]
    [ProducesResponseType(typeof(ArchivedEntityDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDetail(Guid archiveEntryId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _archiveService.GetArchivedDetailAsync(userId, archiveEntryId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve archived entity detail for {ArchiveEntryId}", archiveEntryId);
            return StatusCode(500, new { message = "Failed to retrieve entity detail" });
        }
    }

    [HttpPost("{archiveEntryId}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Restore(Guid archiveEntryId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _archiveService.RestoreEntityAsync(userId, archiveEntryId);
            
            _logger.LogInformation("Restored archive entry {ArchiveEntryId} for user {UserId}", 
                archiveEntryId, userId);
            
            return Ok(new { message = "Entity restored successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore archive entry {ArchiveEntryId}", archiveEntryId);
            return StatusCode(500, new { message = "Failed to restore entity" });
        }
    }

    [HttpDelete("{archiveEntryId}/permanent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PermanentDelete(Guid archiveEntryId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _archiveService.PermanentDeleteAsync(userId, archiveEntryId);
            
            _logger.LogInformation("Permanently deleted archive entry {ArchiveEntryId} for user {UserId}", 
                archiveEntryId, userId);
            
            return Ok(new { message = "Entity permanently deleted" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to permanently delete archive entry {ArchiveEntryId}", archiveEntryId);
            return StatusCode(500, new { message = "Failed to delete entity" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpPost("migrate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MigrateExistingArchives()
    {
        try
        {
            _logger.LogInformation("Migration of archived entities requested");
            
            await _migrationService.MigrateExistingArchivedEntitiesAsync();
            
            return Ok(new { message = "Migration completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate archived entities");
            return StatusCode(500, new { error = "Failed to migrate archived entities", details = ex.Message });
        }
    }
}

