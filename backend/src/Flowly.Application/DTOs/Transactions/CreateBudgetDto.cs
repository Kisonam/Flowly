namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating a new budget
/// </summary>
public class CreateBudgetDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Limit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
}
