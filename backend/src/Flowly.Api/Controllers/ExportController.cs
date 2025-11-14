// backend/src/Flowly.Api/Controllers/ExportController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IExportService exportService, ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    // ============================================
    // GET /api/export
    // ============================================

    /// <summary>
    /// Export all user data as Markdown ZIP archive
    /// </summary>
    /// <returns>ZIP file containing all user data in Markdown format</returns>
    /// <response code="200">Data exported successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportMarkdownZip()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Starting Markdown ZIP export for user: {UserId}", userId);

            var fileBytes = await _exportService.ExportAsMarkdownZipAsync(userId);
            var fileName = $"flowly-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.zip";
            
            _logger.LogInformation("Markdown ZIP export completed for user: {UserId}, file size: {Size} bytes", userId, fileBytes.Length);

            return File(fileBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data as Markdown ZIP");
            return StatusCode(500, new { message = "Failed to export data", error = ex.Message });
        }
    }

    // ============================================
    // GET /api/export/json
    // ============================================

    /// <summary>
    /// Export all user data as JSON file
    /// </summary>
    /// <returns>JSON file containing all user data</returns>
    /// <response code="200">Data exported successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportJson()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Starting JSON export for user: {UserId}", userId);

            var fileBytes = await _exportService.ExportAsJsonAsync(userId);
            var fileName = $"flowly-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
            
            _logger.LogInformation("JSON export completed for user: {UserId}, file size: {Size} bytes", userId, fileBytes.Length);

            return File(fileBytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data as JSON");
            return StatusCode(500, new { message = "Failed to export data", error = ex.Message });
        }
    }

    // ============================================
    // GET /api/export/csv
    // ============================================

    /// <summary>
    /// Export all user data as CSV ZIP archive
    /// </summary>
    /// <returns>ZIP file containing CSV files for each data type</returns>
    /// <response code="200">Data exported successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportCsv()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Starting CSV export for user: {UserId}", userId);

            var fileBytes = await _exportService.ExportAsCsvAsync(userId);
            var fileName = $"flowly-export-csv-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.zip";
            
            _logger.LogInformation("CSV export completed for user: {UserId}, file size: {Size} bytes", userId, fileBytes.Length);

            return File(fileBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data as CSV");
            return StatusCode(500, new { message = "Failed to export data", error = ex.Message });
        }
    }

    // ============================================
    // GET /api/export/pdf
    // ============================================

    /// <summary>
    /// Export all user data as PDF file
    /// </summary>
    /// <returns>PDF file containing user data summary</returns>
    /// <response code="200">Data exported successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportPdf()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Starting PDF export for user: {UserId}", userId);

            var fileBytes = await _exportService.ExportAsPdfAsync(userId);
            var fileName = $"flowly-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.pdf";
            
            _logger.LogInformation("PDF export completed for user: {UserId}, file size: {Size} bytes", userId, fileBytes.Length);

            return File(fileBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data as PDF");
            return StatusCode(500, new { message = "Failed to export data", error = ex.Message });
        }
    }
}

