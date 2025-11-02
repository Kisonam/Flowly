using System;
using System.Security.Claims;

namespace Flowly.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string>? roles = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Guid? GetUserIdFromToken(string token);
    string? GetEmailFromToken(string token);
}
