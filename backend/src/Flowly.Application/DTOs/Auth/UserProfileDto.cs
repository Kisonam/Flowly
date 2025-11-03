using System;
using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Auth;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string AvatarUrl { get; set; } = null!;
    public ThemeMode PreferredTheme { get; set; }
    public DateTime CreatedAt { get; set; }
}
