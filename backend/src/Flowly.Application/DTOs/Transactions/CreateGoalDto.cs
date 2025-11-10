namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating a new financial goal
/// </summary>
public class CreateGoalDto
{
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public string? Description { get; set; }
}
