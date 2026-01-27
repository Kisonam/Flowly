namespace Flowly.Application.DTOs.Transactions;

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
