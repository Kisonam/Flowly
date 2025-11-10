namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for filtering budgets
/// </summary>
public class BudgetFilterDto
{
    public bool? IsActive { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
