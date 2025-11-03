using System;
using Flowly.Application.DTOs.Auth;

namespace Flowly.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress = null);

    Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress = null);

    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, string? ipAddress = null);

    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto, string? ipAddress = null);

    Task RevokeTokenAsync(string refreshToken, string? ipAddress = null);

    Task<UserProfileDto> GetCurrentUserAsync(Guid userId);

    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);

    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

    Task<string> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType);

    Task DeleteAvatarAsync(Guid userId);
}
