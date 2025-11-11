using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/finance/budgets")]
[Produces("application/json")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(IBudgetService budgetService, ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    /// <summary>
    /// Get all budgets with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BudgetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Log received parameters
            _logger.LogInformation("📥 GetAll parameters: isActive={IsActive}, isArchived={IsArchived}, dateFrom={DateFrom}, dateTo={DateTo}, currency={Currency}", 
                isActive, isArchived, dateFrom, dateTo, currencyCode);
            
            var filter = new BudgetFilterDto
            {
                IsActive = isActive,
                IsArchived = isArchived,
                CategoryId = categoryId,
                CurrencyCode = currencyCode,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var budgets = await _budgetService.GetAllAsync(userId, filter);
            _logger.LogInformation("✅ Budgets fetched: {Count} items", budgets.Count);
            return Ok(budgets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get budgets");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get budget by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var budget = await _budgetService.GetByIdAsync(userId, id);
            return Ok(budget);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Budget not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get budget {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new budget
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var budget = await _budgetService.CreateAsync(userId, dto);
            _logger.LogInformation("✅ Budget created: {Id}", budget.Id);
            return CreatedAtAction(nameof(GetById), new { id = budget.Id }, budget);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid budget data: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Budget creation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create budget");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing budget
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var budget = await _budgetService.UpdateAsync(userId, id, dto);
            _logger.LogInformation("✅ Budget updated: {Id}", id);
            return Ok(budget);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Budget not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid budget data: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to update budget {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a budget
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _budgetService.DeleteAsync(userId, id);
            _logger.LogInformation("✅ Budget deleted: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Budget not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to delete budget {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Archive a budget
    /// </summary>
    [HttpPost("{id}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _budgetService.ArchiveAsync(userId, id);
            _logger.LogInformation("✅ Budget archived: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Budget not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to archive budget {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Restore an archived budget
    /// </summary>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _budgetService.RestoreAsync(userId, id);
            _logger.LogInformation("✅ Budget restored: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Budget not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to restore budget {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check if budget is overspent
    /// </summary>
    [HttpGet("{id}/overspent")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IsOverspent(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isOverspent = await _budgetService.IsOverspentAsync(userId, id);
            return Ok(new { budgetId = id, isOverspent });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Budget not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to check budget overspent status {Id}", id);
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
