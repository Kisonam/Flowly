namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating a new category
/// </summary>
public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
