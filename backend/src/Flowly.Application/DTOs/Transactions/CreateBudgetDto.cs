namespace Flowly.Application.DTOs.Transactions;

public class CreateBudgetDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Limit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
}
