using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public class TransactionsController(ITransactionQueryService txService, ILogger<TransactionsController> logger) : ControllerBase
{
    private readonly ITransactionQueryService _txService = txService;
    private readonly ILogger<TransactionsController> _logger = logger;

    /// <summary>
    /// List transactions for current user (lightweight)
    /// </summary>
    /// <param name="search">Search by description or amount</param>
    /// <param name="isArchived">Filter by archive status</param>
    /// <param name="take">Max items (1..100, default 50)</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<TransactionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool? isArchived = null, [FromQuery] int? take = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var items = await _txService.GetListAsync(userId, search, isArchived, take ?? 50);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions");
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
