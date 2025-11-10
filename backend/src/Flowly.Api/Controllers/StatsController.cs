using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/finance/stats")]
[Produces("application/json")]
public class StatsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<StatsController> _logger;

    public StatsController(ITransactionService transactionService, ILogger<StatsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get financial statistics for a specified period
    /// </summary>
    /// <param name="periodStart">Start date of the period (required)</param>
    /// <param name="periodEnd">End date of the period (required)</param>
    /// <param name="currencyCode">Optional: Filter by specific currency (e.g., "UAH", "USD")</param>
    /// <response code="200">Returns aggregated statistics including income/expense by category, monthly trends, and averages</response>
    /// <response code="400">Invalid date range or parameters</response>
    [HttpGet]
    [ProducesResponseType(typeof(FinanceStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd,
        [FromQuery] string? currencyCode = null)
    {
        try
        {
            if (periodStart >= periodEnd)
            {
                return BadRequest(new { message = "Period start must be earlier than period end" });
            }

            var userId = GetCurrentUserId();
            var stats = await _transactionService.GetStatsAsync(userId, periodStart, periodEnd, currencyCode);
            
            _logger.LogInformation(
                "📊 Finance stats generated for period {Start:yyyy-MM-dd} to {End:yyyy-MM-dd} | Currency: {Currency}",
                periodStart, periodEnd, currencyCode ?? "All");
            
            return Ok(stats);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid stats parameters: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate finance statistics");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get quick statistics for current month
    /// </summary>
    /// <param name="currencyCode">Optional: Filter by specific currency</param>
    /// <response code="200">Returns current month statistics</response>
    [HttpGet("current-month")]
    [ProducesResponseType(typeof(FinanceStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentMonthStats([FromQuery] string? currencyCode = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var periodStart = new DateTime(now.Year, now.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var stats = await _transactionService.GetStatsAsync(userId, periodStart, periodEnd, currencyCode);
            
            _logger.LogInformation("📊 Current month stats generated ({Month:yyyy-MM})", now);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate current month statistics");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get quick statistics for current year
    /// </summary>
    /// <param name="currencyCode">Optional: Filter by specific currency</param>
    /// <response code="200">Returns current year statistics</response>
    [HttpGet("current-year")]
    [ProducesResponseType(typeof(FinanceStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentYearStats([FromQuery] string? currencyCode = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var periodStart = new DateTime(now.Year, 1, 1);
            var periodEnd = new DateTime(now.Year, 12, 31);

            var stats = await _transactionService.GetStatsAsync(userId, periodStart, periodEnd, currencyCode);
            
            _logger.LogInformation("📊 Current year stats generated ({Year})", now.Year);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate current year statistics");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get statistics for the last N days
    /// </summary>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <param name="currencyCode">Optional: Filter by specific currency</param>
    /// <response code="200">Returns statistics for the specified period</response>
    [HttpGet("last-days")]
    [ProducesResponseType(typeof(FinanceStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLastDaysStats(
        [FromQuery] int days = 30,
        [FromQuery] string? currencyCode = null)
    {
        try
        {
            if (days <= 0)
            {
                return BadRequest(new { message = "Days must be greater than 0" });
            }

            var userId = GetCurrentUserId();
            var periodEnd = DateTime.UtcNow;
            var periodStart = periodEnd.AddDays(-days);

            var stats = await _transactionService.GetStatsAsync(userId, periodStart, periodEnd, currencyCode);
            
            _logger.LogInformation("📊 Last {Days} days stats generated", days);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate last days statistics");
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
