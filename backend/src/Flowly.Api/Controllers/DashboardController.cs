using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Dashboard;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview data
    /// Returns finance stats for current month, upcoming 5 tasks, and recent 3 notes
    /// </summary>
    /// <response code="200">Dashboard data retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dashboard = await _dashboardService.GetDashboardAsync(userId);
            
            _logger.LogInformation("📊 Dashboard data fetched for user {UserId}", userId);
            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("❌ Unauthorized access to dashboard: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get dashboard data");
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
