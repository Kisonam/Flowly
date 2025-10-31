using System;
using Flowly.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Flowly.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public ThemeMode PreferredTheme { get; set; } = ThemeMode.Normal;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
