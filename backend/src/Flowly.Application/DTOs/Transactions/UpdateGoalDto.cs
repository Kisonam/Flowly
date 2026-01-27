namespace Flowly.Application.DTOs.Transactions;

public class UpdateGoalDto
{
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public string? Description { get; set; }
}
