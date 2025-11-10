using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using Flowly.Domain.Enums;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/finance/transactions")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    // ============================================
    // CRUD Operations
    // ============================================

    /// <summary>
    /// Get all transactions with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] TransactionType? type = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] string? tagIds = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Parse tag IDs
            List<Guid>? tagIdList = null;
            if (!string.IsNullOrWhiteSpace(tagIds))
            {
                tagIdList = tagIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
            }

            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var filter = new TransactionFilterDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Type = type,
                CategoryId = categoryId,
                CurrencyCode = currencyCode,
                TagIds = tagIdList,
                Page = page,
                PageSize = pageSize
            };

            var result = await _transactionService.GetAllAsync(userId, filter);
            _logger.LogInformation("✅ Transactions fetched: {Count} items", result.Items.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get transactions");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.GetByIdAsync(userId, id);
            return Ok(transaction);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Transaction not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get transaction {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new transaction
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.CreateAsync(userId, dto);
            _logger.LogInformation("✅ Transaction created: {Id}", transaction.Id);
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid transaction data: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Transaction creation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create transaction");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.UpdateAsync(userId, id, dto);
            _logger.LogInformation("✅ Transaction updated: {Id}", id);
            return Ok(transaction);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Transaction not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("❌ Invalid transaction data: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to update transaction {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Archive a transaction
    /// </summary>
    [HttpPost("{id}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _transactionService.ArchiveAsync(userId, id);
            _logger.LogInformation("✅ Transaction archived: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Transaction not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to archive transaction {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Restore an archived transaction
    /// </summary>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _transactionService.RestoreAsync(userId, id);
            _logger.LogInformation("✅ Transaction restored: {Id}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("❌ Transaction not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to restore transaction {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================
    // Stats
    // ============================================

    /// <summary>
    /// Get financial statistics for a period
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(FinanceStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd,
        [FromQuery] string? currencyCode = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var stats = await _transactionService.GetStatsAsync(userId, periodStart, periodEnd, currencyCode);
            _logger.LogInformation("✅ Stats fetched for period {Start} - {End}", periodStart, periodEnd);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get stats");
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================
    // Helpers
    // ============================================

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
