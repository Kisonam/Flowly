namespace Flowly.Application.DTOs.Transactions;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
