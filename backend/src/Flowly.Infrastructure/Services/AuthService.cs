using System;
using Flowly.Application.Common.Settings;
using Flowly.Application.DTOs.Auth;
using Flowly.Application.DTOs.Common.Settings;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flowly.Infrastructure.Services;

public class AuthService(UserManager<ApplicationUser> userManager,
                        SignInManager<ApplicationUser> signInManager,
                        IJwtService jwtService,
                        AppDbContext dbContext,
                        IOptions<JwtSettings> jwtSettings,
                        IOptions<GoogleSettings> googleSettings) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IJwtService _jwtService = jwtService;
    private readonly AppDbContext _dbContext = dbContext;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly GoogleSettings _googleSettings = googleSettings.Value;

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress = null)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Create new user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            EmailConfirmed = true, // Auto-confirm for now (can add email verification later)
            CreatedAt = DateTime.UtcNow
        };

        // Create user with password
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Generate tokens
        return await GenerateAuthResponse(user, ipAddress);
    }
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress = null)
    {
        // Find user
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Check password
        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            throw new UnauthorizedAccessException("Account is locked due to multiple failed login attempts");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Generate tokens
        return await GenerateAuthResponse(user, ipAddress);
    }
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Change password
        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to change password: {errors}");
        }

        // Revoke all refresh tokens (force re-login on all devices)
        var now = DateTime.UtcNow;
        var refreshTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now)
            .ToListAsync();

        foreach (var token in refreshTokens)
        {
            token.Revoke();
        }

        await _dbContext.SaveChangesAsync();
    }
    public async Task DeleteAvatarAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Delete file if exists
        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarPath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        // Update user
        user.AvatarPath = null;

        await _userManager.UpdateAsync(user);
    }
    public async Task<UserProfileDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        return MapToUserProfileDto(user);
    }

    //TODO: Implement Google Login
    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, string? ipAddress = null)
    {
      try
        {
            // 1. Validate Google ID token
            var payload = await ValidateGoogleTokenAsync(dto.IdToken);

            if (payload == null)
            {
                throw new UnauthorizedAccessException("Invalid Google token");
            }

            // 2. Check if user already exists
            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                // 3. Create new user if doesn't exist
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = payload.Email,
                    Email = payload.Email,
                    DisplayName = payload.Name ?? payload.Email,
                    EmailConfirmed = true, // Google emails are already verified
                    CreatedAt = DateTime.UtcNow
                };

                // Create user without password (external login)
                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user: {errors}");
                }

                // Add Google login info
                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                await _userManager.AddLoginAsync(user, loginInfo);
            }
            else
            {
                // 4. Check if Google login is already linked
                var logins = await _userManager.GetLoginsAsync(user);
                var googleLogin = logins.FirstOrDefault(l => l.LoginProvider == "Google");

                if (googleLogin == null)
                {
                    // Link Google account to existing user
                    var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                    await _userManager.AddLoginAsync(user, loginInfo);
                }
            }

            // 5. Generate tokens
            return await GenerateAuthResponse(user, ipAddress);
        }
        catch (InvalidJwtException ex)
        {
            throw new UnauthorizedAccessException("Invalid Google token", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Google authentication failed", ex);
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto, string? ipAddress = null)
    {
        // Get user ID from expired access token
        var userId = _jwtService.GetUserIdFromToken(dto.AccessToken);
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Invalid access token");
        }

        // Find refresh token in database
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && rt.UserId == userId.Value);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Check if token is active
        if (!refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is expired or revoked");
        }

        // Get user
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // Revoke old refresh token
        refreshToken.Revoke(ipAddress);

        // Generate new tokens
        var authResponse = await GenerateAuthResponse(user, ipAddress);

        // Save changes
        await _dbContext.SaveChangesAsync();

        return authResponse;
    }

    public async Task RevokeTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null)
        {
            throw new InvalidOperationException("Token not found");
        }

        if (!token.IsActive)
        {
            throw new InvalidOperationException("Token is already inactive");
        }

        // Revoke token
        token.Revoke(ipAddress);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Update fields
        user.DisplayName = dto.DisplayName;

        if (dto.PreferredTheme.HasValue)
        {
            user.PreferredTheme = dto.PreferredTheme.Value;
        }

        // Save changes
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update profile: {errors}");
        }

        return MapToUserProfileDto(user);
    }

    public async Task<string> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(contentType.ToLower()))
        {
            throw new InvalidOperationException("Invalid file type. Only JPEG, PNG, and GIF are allowed");
        }

        // Validate file size (max 5MB)
        if (fileStream.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException("File size exceeds 5MB limit");
        }

        // Generate unique filename
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{userId}_avatar_{Guid.NewGuid()}{extension}";
        var uploadPath = Path.Combine("uploads", "avatars", userId.ToString());
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", uploadPath);

        // Create directory if not exists
        Directory.CreateDirectory(fullPath);

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarPath.TrimStart('/'));
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }

        // Save file
        var filePath = Path.Combine(fullPath, uniqueFileName);
        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        // Update user avatar path
        var avatarUrl = $"/uploads/avatars/{userId}/{uniqueFileName}";
        user.AvatarPath = avatarUrl;

        await _userManager.UpdateAsync(user);

        return avatarUrl;
    }

    // Private Helper Methods
    private async Task<AuthResponseDto> GenerateAuthResponse(ApplicationUser user, string? ipAddress = null)
    {
        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate access token
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);

        // Generate refresh token
        var refreshTokenString = _jwtService.GenerateRefreshToken();

        // Create refresh token entity
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        // Save refresh token
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        // Return response
        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60, // Convert to seconds
            User = MapToUserProfileDto(user)
        };
    }

    private UserProfileDto MapToUserProfileDto(ApplicationUser user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarPath,
            PreferredTheme = user.PreferredTheme,
            CreatedAt = user.CreatedAt
        };
    }
    // Validate Google ID token
    private async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleSettings.ClientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return payload;
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
