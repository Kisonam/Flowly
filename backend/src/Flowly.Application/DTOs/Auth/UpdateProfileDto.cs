using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Auth;

public class UpdateProfileDto
{
    public string DisplayName { get; set; } = string.Empty;
    public ThemeMode? PreferredTheme { get; set; }
}
