namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for updating an existing financial goal
/// </summary>
public class UpdateGoalDto
{
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public string? Description { get; set; }
}
