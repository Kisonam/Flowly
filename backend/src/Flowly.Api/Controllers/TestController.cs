

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.Interfaces;

namespace Flowly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IJwtService _jwtService;

    public TestController(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    [HttpGet("generate-token")]
    public IActionResult GenerateToken()
    {
        var userId = Guid.NewGuid();
        var email = "test@flowly.com";

        var accessToken = _jwtService.GenerateAccessToken(userId, email);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return Ok(new
        {
            accessToken,
            refreshToken,
            expiresIn = 900 
        });
    }

    [Authorize]
    [HttpGet("protected")]
    public IActionResult Protected()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        return Ok(new
        {
            message = "You are authenticated!",
            userId,
            email
        });
    }
}