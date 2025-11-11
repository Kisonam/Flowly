using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Flowly.Infrastructure.Data;

namespace Flowly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CurrenciesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CurrenciesController> _logger;

    public CurrenciesController(AppDbContext dbContext, ILogger<CurrenciesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all available currencies
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var currencies = await _dbContext.Currencies
                .OrderBy(c => c.Code)
                .ToListAsync();

            _logger.LogInformation("✅ Currencies fetched: {Count} items", currencies.Count);
            return Ok(currencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get currencies");
            return BadRequest(new { message = ex.Message });
        }
    }
}
