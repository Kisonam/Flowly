namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Budget summary DTO for transaction display
/// </summary>
public class BudgetSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}
