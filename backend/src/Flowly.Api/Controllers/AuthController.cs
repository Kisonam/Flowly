// backend/src/Flowly.Api/Controllers/AuthController.cs

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

    // ============================================
    // POST /api/auth/register
    // ============================================

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="dto">Registration details</param>
    /// <returns>Authentication response with tokens</returns>
    /// <response code="200">User registered successfully</response>
    /// <response code="400">Validation error or user already exists</response>
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

    // ============================================
    // POST /api/auth/login
    // ============================================

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <returns>Authentication response with tokens</returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid credentials</response>
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

    // ============================================
    // POST /api/auth/google
    // ============================================

    /// <summary>
    /// Login or register with Google OAuth
    /// </summary>
    /// <param name="dto">Google ID token</param>
    /// <returns>Authentication response with tokens</returns>
    /// <response code="200">Login successful</response>
    /// <response code="400">Invalid Google token</response>
    /// <response code="501">Not implemented yet</response>
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

    // ============================================
    // POST /api/auth/refresh
    // ============================================

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="dto">Access and refresh tokens</param>
    /// <returns>New authentication response with tokens</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Invalid or expired refresh token</response>
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

    // ============================================
    // POST /api/auth/revoke
    // ============================================

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    /// <param name="dto">Refresh token to revoke</param>
    /// <returns>Success message</returns>
    /// <response code="200">Token revoked successfully</response>
    /// <response code="400">Invalid token</response>
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

    // ============================================
    // GET /api/auth/me
    // ============================================

    /// <summary>
    /// Get current authenticated user profile
    /// </summary>
    /// <returns>User profile</returns>
    /// <response code="200">Profile retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
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

    // ============================================
    // PUT /api/auth/profile
    // ============================================

    /// <summary>
    /// Update current user profile
    /// </summary>
    /// <param name="dto">Profile update data</param>
    /// <returns>Updated profile</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Not authenticated</response>
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

    // ============================================
    // POST /api/auth/change-password
    // ============================================

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="dto">Password change data</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Validation error or incorrect current password</response>
    /// <response code="401">Not authenticated</response>
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

    // ============================================
    // POST /api/auth/avatar
    // ============================================

    /// <summary>
    /// Upload user avatar
    /// </summary>
    /// <param name="file">Image file (max 5MB, jpg/png/gif)</param>
    /// <returns>Avatar URL</returns>
    /// <response code="200">Avatar uploaded successfully</response>
    /// <response code="400">Invalid file or validation error</response>
    /// <response code="401">Not authenticated</response>
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

    // ============================================
    // DELETE /api/auth/avatar
    // ============================================

    /// <summary>
    /// Delete user avatar
    /// </summary>
    /// <returns>Success message</returns>
    /// <response code="200">Avatar deleted successfully</response>
    /// <response code="401">Not authenticated</response>
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

    // ============================================
    // Helper Methods
    // ============================================

    /// <summary>
    /// Get current user ID from JWT claims
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// Get client IP address
    /// </summary>
    private string? GetIpAddress()
    {
        // Try to get IP from X-Forwarded-For header (if behind proxy/load balancer)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }

        // Get IP from connection
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}