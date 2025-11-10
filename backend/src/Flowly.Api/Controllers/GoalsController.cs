using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/finance/goals")]
[Produces("application/json")]
public class GoalsController : ControllerBase
{
    private readonly IFinancialGoalService _goalService;
    private readonly ILogger<GoalsController> _logger;

    public GoalsController(IFinancialGoalService goalService, ILogger<GoalsController> logger)
    {
        _goalService = goalService;
        _logger = logger;
    }

    /// <summary>
    /// Get all financial goals with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FinancialGoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isCompleted = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] DateTime? deadlineFrom = null,
        [FromQuery] DateTime? deadlineTo = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var filter = new GoalFilterDto
            {
                IsCompleted = isCompleted,
                IsArchived = isArchived,
                CurrencyCode = currencyCode,
                DeadlineFrom = deadlineFrom,
                DeadlineTo = deadlineTo
            };

            var goals = await _goalService.GetAllAsync(userId, filter);
            _logger.LogInformation("✅ Financial goals fetched: {Count} items", goals.Count);
            return Ok(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get financial goals");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get financial goal by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var goal = await _goalService.GetByIdAsync(userId, id);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get financial goal {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new financial goal
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGoalDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var goal = await _goalService.CreateAsync(userId, dto);
            _logger.LogInformation("✅ Financial goal created: {Id} - {Title}", goal.Id, goal.Title);
            return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid goal data: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Goal creation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create financial goal");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing financial goal
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGoalDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var goal = await _goalService.UpdateAsync(userId, id, dto);
            _logger.LogInformation("✅ Financial goal updated: {Id} - {Title}", id, goal.Title);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid goal data: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to update financial goal {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a financial goal
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _goalService.DeleteAsync(userId, id);
            _logger.LogInformation("✅ Financial goal deleted: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to delete financial goal {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Archive a financial goal
    /// </summary>
    [HttpPost("{id}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _goalService.ArchiveAsync(userId, id);
            _logger.LogInformation("✅ Financial goal archived: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to archive financial goal {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Restore an archived financial goal
    /// </summary>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _goalService.RestoreAsync(userId, id);
            _logger.LogInformation("✅ Financial goal restored: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to restore financial goal {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================
    // Progress Management
    // ============================================

    /// <summary>
    /// Add amount to goal progress
    /// </summary>
    [HttpPost("{id}/add-amount")]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAmount(Guid id, [FromBody] UpdateGoalAmountDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var goal = await _goalService.AddAmountAsync(userId, id, dto);
            _logger.LogInformation("✅ Amount added to goal {Id}: +{Amount}", id, dto.Amount);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid amount: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to add amount to goal {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Subtract amount from goal progress
    /// </summary>
    [HttpPost("{id}/subtract-amount")]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubtractAmount(Guid id, [FromBody] UpdateGoalAmountDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var goal = await _goalService.SubtractAmountAsync(userId, id, dto);
            _logger.LogInformation("✅ Amount subtracted from goal {Id}: -{Amount}", id, dto.Amount);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid amount: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to subtract amount from goal {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Set current amount for goal
    /// </summary>
    [HttpPost("{id}/set-amount")]
    [ProducesResponseType(typeof(FinancialGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCurrentAmount(Guid id, [FromBody] UpdateGoalAmountDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var goal = await _goalService.SetCurrentAmountAsync(userId, id, dto);
            _logger.LogInformation("✅ Current amount set for goal {Id}: {Amount}", id, dto.Amount);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Financial goal not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid amount: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to set amount for goal {Id}", id);
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
