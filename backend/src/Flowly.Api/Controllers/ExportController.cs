// backend/src/Flowly.Api/Controllers/ExportController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;

namespace Flowly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ExportController : ControllerBase
{
    private readonly ILogger<ExportController> _logger;

    public ExportController(ILogger<ExportController> logger)
    {
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
    public async Task<IActionResult> ExportAllData()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Starting data export for user: {UserId}", userId);

            // Create a memory stream for the ZIP file
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Add README file
                var readmeEntry = archive.CreateEntry("README.md");
                using (var entryStream = readmeEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteLineAsync("# Flowly Data Export");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync($"Export Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    await writer.WriteLineAsync($"User ID: {userId}");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("## Contents");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("This archive contains all your data from Flowly in Markdown format.");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("- `notes/` - All your notes");
                    await writer.WriteLineAsync("- `tasks/` - All your tasks");
                    await writer.WriteLineAsync("- `transactions/` - All your transactions");
                    await writer.WriteLineAsync("- `budgets/` - All your budgets");
                    await writer.WriteLineAsync("- `goals/` - All your goals");
                }

                // Add sample data files (placeholders for now)
                // TODO: Implement actual data export from services
                
                var notesEntry = archive.CreateEntry("notes/sample-note.md");
                using (var entryStream = notesEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteLineAsync("# Sample Note");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("This is a sample note export.");
                    await writer.WriteLineAsync("Full implementation coming soon.");
                }

                var tasksEntry = archive.CreateEntry("tasks/tasks.md");
                using (var entryStream = tasksEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteLineAsync("# Tasks");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("Your tasks will be exported here.");
                }

                var transactionsEntry = archive.CreateEntry("transactions/transactions.md");
                using (var entryStream = transactionsEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    await writer.WriteLineAsync("# Transactions");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("Your financial transactions will be exported here.");
                }
            }

            memoryStream.Position = 0;
            var fileBytes = memoryStream.ToArray();

            var fileName = $"flowly-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.zip";
            
            _logger.LogInformation("Data export completed for user: {UserId}, file size: {Size} bytes", userId, fileBytes.Length);

            return File(fileBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data");
            return StatusCode(500, new { message = "Failed to export data", error = ex.Message });
        }
    }
}
