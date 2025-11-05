using System.ComponentModel.DataAnnotations;

namespace Flowly.Application.DTOs.Tags;

public class UpdateTagDto
{
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Tag name must be between 1 and 50 characters")]
    public string? Name { get; set; }

    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Color must be a valid hex color (e.g., #FF5733 or #F57)")]
    public string? Color { get; set; }
}
