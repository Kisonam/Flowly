

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Flowly.Application.DTOs.Auth;
using Flowly.Application.DTOs.Common;
using Flowly.Application.Interfaces;
using System.Security.Claims;

namespace Flowly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.RegisterAsync(dto, ipAddress);

            _logger.LogInformation("User registered successfully: {Email}", dto.Email);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.LoginAsync(dto, ipAddress);

            _logger.LogInformation("User logged in successfully: {Email}", dto.Email);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: {Message}", ex.Message);
            return Unauthorized(new ErrorResponse
            {
                StatusCode = 401,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.GoogleLoginAsync(dto, ipAddress);

            _logger.LogInformation("User logged in with Google");

            return Ok(response);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(501, new ErrorResponse
            {
                StatusCode = 501,
                Message = "Google OAuth integration not yet implemented",
                Details = ex.Message,
                Path = Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google login failed");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = "Google login failed",
                Details = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.RefreshTokenAsync(dto, ipAddress);

            _logger.LogInformation("Token refreshed successfully");

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new ErrorResponse
            {
                StatusCode = 401,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var ipAddress = GetIpAddress();
            await _authService.RevokeTokenAsync(dto.RefreshToken, ipAddress);

            _logger.LogInformation("Token revoked successfully");

            return Ok(new { message = "Token revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Token revocation failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var profile = await _authService.GetCurrentUserAsync(userId);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [Authorize]
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var profile = await _authService.UpdateProfileAsync(userId, dto);

            _logger.LogInformation("Profile updated for user: {UserId}", userId);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _authService.ChangePasswordAsync(userId, dto);

            _logger.LogInformation("Password changed for user: {UserId}", userId);

            return Ok(new { message = "Password changed successfully. Please login again on all devices." });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Password change failed: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [Authorize]
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = 400,
                    Message = "No file uploaded",
                    Path = Request.Path
                });
            }

            var userId = GetCurrentUserId();

            using var stream = file.OpenReadStream();
            var avatarUrl = await _authService.UploadAvatarAsync(
                userId,
                stream,
                file.FileName,
                file.ContentType
            );

            _logger.LogInformation("Avatar uploaded for user: {UserId}", userId);

            return Ok(new { avatarUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Avatar upload failed");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    [Authorize]
    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAvatar()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _authService.DeleteAvatarAsync(userId);

            _logger.LogInformation("Avatar deleted for user: {UserId}", userId);

            return Ok(new { message = "Avatar deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Avatar deletion failed");
            return BadRequest(new ErrorResponse
            {
                StatusCode = 400,
                Message = ex.Message,
                Path = Request.Path
            });
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

    private string? GetIpAddress()
    {
        
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}