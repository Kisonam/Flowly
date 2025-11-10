namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for updating an existing budget
/// </summary>
public class UpdateBudgetDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Limit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
}
