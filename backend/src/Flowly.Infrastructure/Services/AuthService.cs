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
using Microsoft.Extensions.Configuration;

namespace Flowly.Infrastructure.Services;

public class AuthService(UserManager<ApplicationUser> userManager,
                        SignInManager<ApplicationUser> signInManager,
                        IJwtService jwtService,
                        AppDbContext dbContext,
                        IOptions<JwtSettings> jwtSettings,
                        IOptions<GoogleSettings> googleSettings,
                        IConfiguration configuration) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IJwtService _jwtService = jwtService;
    private readonly AppDbContext _dbContext = dbContext;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly GoogleSettings _googleSettings = googleSettings.Value;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress = null)
    {
        
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            EmailConfirmed = true, 
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        return await GenerateAuthResponse(user, ipAddress);
    }
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress = null)
    {
        
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            throw new UnauthorizedAccessException("Account is locked due to multiple failed login attempts");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        return await GenerateAuthResponse(user, ipAddress);
    }
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to change password: {errors}");
        }

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

        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarPath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

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

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, string? ipAddress = null)
    {
      try
        {
            
            var payload = await ValidateGoogleTokenAsync(dto.IdToken);
            if (payload == null)
            {
                throw new UnauthorizedAccessException("Invalid Google token");
            }
            
            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user == null)
            {
                
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = payload.Email,
                    Email = payload.Email,
                    DisplayName = payload.Name ?? payload.Email,
                    EmailConfirmed = true, 
                    CreatedAt = DateTime.UtcNow
                };
                
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user: {errors}");
                }
                
                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                await _userManager.AddLoginAsync(user, loginInfo);
            }
            else
            {
                
                var logins = await _userManager.GetLoginsAsync(user);
                var googleLogin = logins.FirstOrDefault(l => l.LoginProvider == "Google");

                if (googleLogin == null)
                {
                    
                    var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                    await _userManager.AddLoginAsync(user, loginInfo);
                }
            }
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
        
        var userId = _jwtService.GetUserIdFromToken(dto.AccessToken);
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Invalid access token");
        }

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && rt.UserId == userId.Value);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (!refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is expired or revoked");
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        refreshToken.Revoke(ipAddress);

        var authResponse = await GenerateAuthResponse(user, ipAddress);

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

        user.DisplayName = dto.DisplayName;

        if (dto.PreferredTheme.HasValue)
        {
            user.PreferredTheme = dto.PreferredTheme.Value;
        }

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

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(contentType.ToLower()))
        {
            throw new InvalidOperationException("Invalid file type. Only JPEG, PNG, and GIF are allowed");
        }

        if (fileStream.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException("File size exceeds 5MB limit");
        }

        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{userId}_avatar_{Guid.NewGuid()}{extension}";

        var uploadsBasePath = _configuration["FileStorage:Path"] ?? "/app/uploads";
        var uploadPath = Path.Combine(uploadsBasePath, "avatars", userId.ToString());

        Directory.CreateDirectory(uploadPath);

        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            var oldPath = Path.Combine(uploadsBasePath, user.AvatarPath.TrimStart('/').Replace("uploads/", ""));
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }

        var filePath = Path.Combine(uploadPath, uniqueFileName);
        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        var avatarUrl = $"/uploads/avatars/{userId}/{uniqueFileName}";
        user.AvatarPath = avatarUrl;

        await _userManager.UpdateAsync(user);

        return avatarUrl;
    }

    private async Task<AuthResponseDto> GenerateAuthResponse(ApplicationUser user, string? ipAddress = null)
    {
        
        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles);

        var refreshTokenString = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60, 
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
