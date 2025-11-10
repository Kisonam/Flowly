namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Category information DTO
/// </summary>
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
