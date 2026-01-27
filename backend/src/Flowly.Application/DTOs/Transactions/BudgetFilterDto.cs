namespace Flowly.Application.DTOs.Transactions;

public class BudgetFilterDto
{
    public bool? IsActive { get; set; }
    public bool? IsArchived { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
